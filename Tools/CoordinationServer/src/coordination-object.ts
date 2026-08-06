import type { Env } from './env';
import { hasAdministratorToken, isDisplayName, readBearerToken } from './auth/admin';
import {
  constantTimeEquals,
  createDeveloperTokenDigest,
  createSessionTokenDigest,
  generateOpaqueToken
} from './auth/crypto';
import {
  isExpired,
  LeaseTtlSeconds,
  ReservationTtlSeconds,
  sessionExpiry
} from './auth/session';

interface DeveloperRow extends Record<string, string> {
  developer_id: string;
  display_name: string;
  token_digest: string;
}

interface SessionRow extends Record<string, string> {
  developer_id: string;
  display_name: string;
  token_digest: string;
  expires_at: string;
}

export class CoordinationObject {
  private readonly initialized: Promise<void>;

  constructor(
    readonly state: DurableObjectState,
    readonly env: Env
  ) {
    this.initialized = state.blockConcurrencyWhile(() => this.initialize());
  }

  async fetch(request: Request): Promise<Response> {
    await this.initialized;
    const route = parseRoute(new URL(request.url).pathname);
    if (route === null) {
      return new Response('Not implemented', { status: 501 });
    }

    if (route.kind === 'developers' && request.method === 'POST') {
      return this.createDeveloper(request);
    }
    if (route.kind === 'developer' && request.method === 'DELETE') {
      return this.revokeDeveloper(request, route.developerId);
    }
    if (route.kind === 'sessions' && request.method === 'POST') {
      return this.createSession(request);
    }
    if (route.kind === 'connect' && request.method === 'GET') {
      return await this.authenticateSession(request) === null
        ? unauthorized()
        : new Response('Not implemented', { status: 501 });
    }

    return new Response('Not implemented', { status: 501 });
  }

  private async initialize(): Promise<void> {
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS developers (
        developer_id TEXT PRIMARY KEY,
        display_name TEXT NOT NULL,
        token_digest TEXT NOT NULL UNIQUE,
        revoked_at TEXT
      )
    `);
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS sessions (
        session_id TEXT PRIMARY KEY,
        developer_id TEXT NOT NULL,
        token_digest TEXT NOT NULL UNIQUE,
        expires_at TEXT NOT NULL,
        FOREIGN KEY (developer_id) REFERENCES developers(developer_id)
      )
    `);
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS coordination_state (
        key TEXT PRIMARY KEY,
        value INTEGER NOT NULL
      )
    `);
    this.state.storage.sql.exec(
      "INSERT OR IGNORE INTO coordination_state (key, value) VALUES ('state_version', 0)"
    );
  }

  private async createDeveloper(request: Request): Promise<Response> {
    if (!hasAdministratorToken(request, this.env.ADMIN_TOKEN)) {
      return unauthorized();
    }

    const body = await readJson(request);
    if (body === null || !isDisplayName(body.displayName)) {
      return badRequest();
    }

    const developerId = crypto.randomUUID();
    const developerToken = generateOpaqueToken();
    const tokenDigest = await createDeveloperTokenDigest(
      this.env.TOKEN_HMAC_KEY,
      developerToken,
      developerId,
      body.displayName
    );
    this.state.storage.sql.exec(
      'INSERT INTO developers (developer_id, display_name, token_digest, revoked_at) VALUES (?, ?, ?, NULL)',
      developerId,
      body.displayName,
      tokenDigest
    );

    return Response.json({ developerId, displayName: body.displayName, developerToken }, { status: 201 });
  }

  private async createSession(request: Request): Promise<Response> {
    const developer = await this.authenticateDeveloper(request);
    if (developer === null) {
      return unauthorized();
    }

    const serverTime = new Date();
    const expiresAt = sessionExpiry(serverTime);
    const sessionToken = generateOpaqueToken();
    const tokenDigest = await createSessionTokenDigest(
      this.env.TOKEN_HMAC_KEY,
      sessionToken,
      developer.developer_id,
      expiresAt
    );
    this.state.storage.sql.exec(
      'INSERT INTO sessions (session_id, developer_id, token_digest, expires_at) VALUES (?, ?, ?, ?)',
      crypto.randomUUID(),
      developer.developer_id,
      tokenDigest,
      expiresAt
    );

    return Response.json({
      developerId: developer.developer_id,
      displayName: developer.display_name,
      sessionToken,
      serverTime: serverTime.toISOString(),
      leaseTtlSeconds: LeaseTtlSeconds,
      reservationTtlSeconds: ReservationTtlSeconds,
      stateVersion: this.stateVersion()
    }, { status: 201 });
  }

  private async revokeDeveloper(request: Request, developerId: string): Promise<Response> {
    if (!hasAdministratorToken(request, this.env.ADMIN_TOKEN)) {
      return unauthorized();
    }

    const revokedAt = new Date().toISOString();
    const exists = this.state.storage.sql.exec(
      'SELECT developer_id FROM developers WHERE developer_id = ?',
      developerId
    ).toArray().length > 0;
    if (!exists) {
      return new Response('Not found', { status: 404 });
    }

    this.state.storage.sql.exec(
      'UPDATE developers SET revoked_at = ? WHERE developer_id = ? AND revoked_at IS NULL',
      revokedAt,
      developerId
    );
    this.state.storage.sql.exec('DELETE FROM sessions WHERE developer_id = ?', developerId);
    return new Response(null, { status: 204 });
  }

  private async authenticateDeveloper(request: Request): Promise<DeveloperRow | null> {
    const token = readBearerToken(request);
    if (token === null) {
      return null;
    }

    const developers = this.state.storage.sql.exec<DeveloperRow>(
      'SELECT developer_id, display_name, token_digest FROM developers WHERE revoked_at IS NULL'
    ).toArray();
    for (const developer of developers) {
      const digest = await createDeveloperTokenDigest(
        this.env.TOKEN_HMAC_KEY,
        token,
        developer.developer_id,
        developer.display_name
      );
      if (constantTimeEquals(digest, developer.token_digest)) {
        return developer;
      }
    }

    return null;
  }

  private async authenticateSession(request: Request): Promise<SessionRow | null> {
    const token = readBearerToken(request);
    if (token === null) {
      return null;
    }

    const serverTime = new Date();
    const sessions = this.state.storage.sql.exec<SessionRow>(`
      SELECT sessions.developer_id, developers.display_name, sessions.token_digest, sessions.expires_at
      FROM sessions INNER JOIN developers ON developers.developer_id = sessions.developer_id
      WHERE developers.revoked_at IS NULL
    `).toArray();
    for (const session of sessions) {
      if (isExpired(session.expires_at, serverTime)) {
        continue;
      }

      const digest = await createSessionTokenDigest(
        this.env.TOKEN_HMAC_KEY,
        token,
        session.developer_id,
        session.expires_at
      );
      if (constantTimeEquals(digest, session.token_digest)) {
        return session;
      }
    }

    return null;
  }

  private stateVersion(): number {
    return this.state.storage.sql.exec<{ value: number }>(
      "SELECT value FROM coordination_state WHERE key = 'state_version'"
    ).one().value;
  }
}

type Route =
  | { kind: 'developers' }
  | { kind: 'developer'; developerId: string }
  | { kind: 'sessions' }
  | { kind: 'connect' };

function parseRoute(pathname: string): Route | null {
  const match = /^\/v1\/projects\/[^/]+\/(developers|sessions|connect)(?:\/([^/]+))?$/.exec(pathname);
  if (match === null) {
    return null;
  }

  if (match[1] === 'developers') {
    return match[2] === undefined ? { kind: 'developers' } : { kind: 'developer', developerId: match[2] };
  }
  if (match[2] !== undefined) {
    return null;
  }
  return match[1] === 'sessions' ? { kind: 'sessions' } : { kind: 'connect' };
}

async function readJson(request: Request): Promise<Record<string, unknown> | null> {
  try {
    const value: unknown = await request.json();
    return typeof value === 'object' && value !== null && !Array.isArray(value)
      ? value as Record<string, unknown>
      : null;
  } catch {
    return null;
  }
}

function unauthorized(): Response {
  return new Response('Unauthorized', { status: 401 });
}

function badRequest(): Response {
  return new Response('Invalid request', { status: 400 });
}
