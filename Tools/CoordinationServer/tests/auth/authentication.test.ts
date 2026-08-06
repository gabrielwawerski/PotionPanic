import { SELF, env, reset, runInDurableObject } from 'cloudflare:test';
import { afterEach, describe, expect, it } from 'vitest';
import { readBearerToken } from '../../src/auth/admin';
import {
  createDeveloperTokenDigest,
  createSessionTokenDigest
} from '../../src/auth/crypto';

const projectId = 'potion-panic';
const projectUrl = `https://example.test/v1/projects/${projectId}`;
const adminToken = 'test-admin-token';

afterEach(async () => {
  await reset();
});

describe('developer administration', () => {
  it('issues distinct 32-byte developer tokens and persists only their digests', async () => {
    const first = await createDeveloper('Rin');
    const second = await createDeveloper('Sol');

    expect(first.developerToken).toMatch(/^[A-Za-z0-9_-]{43}$/);
    expect(second.developerToken).toMatch(/^[A-Za-z0-9_-]{43}$/);
    expect(first.developerToken).not.toBe(second.developerToken);

    const persisted = await inspectProject((state) => ({
      developers: state.storage.sql.exec<{
        developer_id: string;
        display_name: string;
        token_digest: string;
        revoked_at: string | null;
      }>('SELECT developer_id, display_name, token_digest, revoked_at FROM developers').toArray(),
      tables: state.storage.sql.exec<{ name: string }>(
        "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name"
      ).toArray(),
      stateVersion: state.storage.sql.exec<{ value: number }>(
        "SELECT value FROM coordination_state WHERE key = 'state_version'"
      ).one()
    }));

    expect(persisted.tables.map(({ name }) => name)).toEqual([
      'coordination_state', 'developers', 'sessions'
    ]);
    expect(persisted.stateVersion).toEqual({ value: 0 });
    expect(persisted.developers).toEqual(expect.arrayContaining([
      expect.objectContaining({ developer_id: first.developerId, display_name: 'Rin', revoked_at: null }),
      expect.objectContaining({ developer_id: second.developerId, display_name: 'Sol', revoked_at: null })
    ]));
    expect(JSON.stringify(persisted)).not.toContain(first.developerToken);
    expect(JSON.stringify(persisted)).not.toContain(second.developerToken);
    expect(persisted.developers.every(({ token_digest }) => token_digest.length === 64)).toBe(true);
  });

  it('rejects requests without the independent administrator token', async () => {
    const response = await SELF.fetch(`${projectUrl}/developers`, {
      method: 'POST',
      headers: jsonHeaders('wrong-admin-token'),
      body: JSON.stringify({ displayName: 'Rin' })
    });

    expect(response.status).toBe(401);
    await expect(response.text()).resolves.not.toContain('wrong-admin-token');
  });
});

describe('opaque sessions', () => {
  it('creates a 24-hour session without a connection ID', async () => {
    const developer = await createDeveloper('Rin');

    const response = await SELF.fetch(`${projectUrl}/sessions`, {
      method: 'POST',
      headers: bearerHeaders(developer.developerToken)
    });

    expect(response.status).toBe(201);
    const body = await response.json() as Record<string, unknown>;
    expect(body).toEqual({
      developerId: developer.developerId,
      displayName: 'Rin',
      sessionToken: expect.stringMatching(/^[A-Za-z0-9_-]{43}$/),
      serverTime: expect.any(String),
      leaseTtlSeconds: 120,
      reservationTtlSeconds: 1800,
      stateVersion: 0
    });
    expect(body).not.toHaveProperty('connectionId');

    const sessionToken = body.sessionToken as string;
    const session = await inspectProject((state) => state.storage.sql.exec<{
      developer_id: string;
      token_digest: string;
      expires_at: string;
    }>('SELECT developer_id, token_digest, expires_at FROM sessions').one());
    expect(session.developer_id).toBe(developer.developerId);
    expect(session.token_digest).toHaveLength(64);
    expect(JSON.stringify(session)).not.toContain(sessionToken);
    expect(Date.parse(session.expires_at) - Date.now()).toBeGreaterThan(23 * 60 * 60 * 1000);
    expect(Date.parse(session.expires_at) - Date.now()).toBeLessThanOrEqual(24 * 60 * 60 * 1000);
  });

  it('rejects invalid, cross-project, expired, and revoked credentials without leaking tokens', async () => {
    const developer = await createDeveloper('Rin');
    const session = await createSession(developer.developerToken);

    const invalid = await SELF.fetch(`${projectUrl}/sessions`, {
      method: 'POST',
      headers: bearerHeaders('invalid-developer-token')
    });
    expect(invalid.status).toBe(401);
    await expect(invalid.text()).resolves.not.toContain('invalid-developer-token');

    const crossProject = await SELF.fetch('https://example.test/v1/projects/other-project/connect', {
      headers: bearerHeaders(session.sessionToken)
    });
    expect(crossProject.status).toBe(401);

    await inspectProject((state) => {
      state.storage.sql.exec(
        "UPDATE sessions SET expires_at = '2000-01-01T00:00:00.000Z'"
      );
    });
    const expired = await SELF.fetch(`${projectUrl}/connect`, {
      headers: bearerHeaders(session.sessionToken)
    });
    expect(expired.status).toBe(401);

    const freshSession = await createSession(developer.developerToken);
    const revoked = await SELF.fetch(`${projectUrl}/developers/${developer.developerId}`, {
      method: 'DELETE',
      headers: bearerHeaders(adminToken)
    });
    expect(revoked.status).toBe(204);
    const repeatedRevocation = await SELF.fetch(
      `${projectUrl}/developers/${developer.developerId}`,
      { method: 'DELETE', headers: bearerHeaders(adminToken) }
    );
    expect(repeatedRevocation.status).toBe(204);
    const afterRevocation = await SELF.fetch(`${projectUrl}/connect`, {
      headers: bearerHeaders(freshSession.sessionToken)
    });
    expect(afterRevocation.status).toBe(401);
    const persisted = await inspectProject((state) => ({
      developer: state.storage.sql.exec<{ revoked_at: string | null }>(
        'SELECT revoked_at FROM developers WHERE developer_id = ?', developer.developerId
      ).one(),
      sessions: state.storage.sql.exec<{ count: number }>(
        'SELECT COUNT(*) AS count FROM sessions WHERE developer_id = ?', developer.developerId
      ).one()
    }));
    expect(persisted.developer.revoked_at).toEqual(expect.any(String));
    expect(persisted.sessions.count).toBe(0);
  });
});

describe('credential parsing and digests', () => {
  it('rejects malformed opaque bearer tokens before authentication scans persisted records', () => {
    expect(readBearerToken(new Request('https://example.test', {
      headers: bearerHeaders('too-short')
    }))).toBeNull();
  });

  it('uses separate HMAC domains for developer and session token digests', async () => {
    const [developerDigest, sessionDigest] = await Promise.all([
      createDeveloperTokenDigest('test-hmac-key', 'a'.repeat(43), 'developer-1', 'Rin'),
      createSessionTokenDigest('test-hmac-key', 'a'.repeat(43), 'developer-1', 'Rin')
    ]);

    expect(developerDigest).not.toBe(sessionDigest);
  });
});

async function createDeveloper(displayName: string): Promise<{
  developerId: string;
  displayName: string;
  developerToken: string;
}> {
  const response = await SELF.fetch(`${projectUrl}/developers`, {
    method: 'POST',
    headers: jsonHeaders(adminToken),
    body: JSON.stringify({ displayName })
  });

  expect(response.status).toBe(201);
  const body = await response.json() as Record<string, unknown>;
  expect(body).toEqual({
    developerId: expect.any(String),
    displayName,
    developerToken: expect.stringMatching(/^[A-Za-z0-9_-]{43}$/)
  });
  return body as {
    developerId: string;
    displayName: string;
    developerToken: string;
  };
}

async function createSession(developerToken: string): Promise<{ sessionToken: string }> {
  const response = await SELF.fetch(`${projectUrl}/sessions`, {
    method: 'POST',
    headers: bearerHeaders(developerToken)
  });

  expect(response.status).toBe(201);
  return response.json();
}

async function inspectProject<T>(callback: (state: DurableObjectState) => T | Promise<T>): Promise<T> {
  const objectNamespace = (env as unknown as {
    COORDINATION_OBJECT: DurableObjectNamespace;
  }).COORDINATION_OBJECT;
  const object = objectNamespace.get(objectNamespace.idFromName(projectId));
  return runInDurableObject(object, (_instance, state) => callback(state));
}

function bearerHeaders(token: string): HeadersInit {
  return { authorization: `Bearer ${token}` };
}

function jsonHeaders(token: string): HeadersInit {
  return { ...bearerHeaders(token), 'content-type': 'application/json' };
}
