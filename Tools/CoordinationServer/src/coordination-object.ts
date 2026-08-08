import type { Env } from './env';
import { hasAdministratorToken, isDisplayName, readBearerToken } from './auth/admin';
import {
  constantTimeEquals,
  createDeveloperTokenDigest,
  createSessionTokenDigest,
  createTokenLookup,
  generateOpaqueToken
} from './auth/crypto';
import {
  isExpired,
  LeaseTtlSeconds,
  ReservationTtlSeconds,
  sessionExpiry
} from './auth/session';
import {
  canonicalPathKey,
  MaximumEnvelopeBytes,
  normalizePath,
  parseClientEnvelope,
  ProtocolVersion,
  type ClientEnvelope,
  type LeaseRecord,
  type PresenceRecord,
  type ServerEnvelope
} from './protocol';

const ReplayTtlMilliseconds = 5 * 60 * 1000;
const MaximumSnapshotDataBytes = 256 * 1024;
const CapacitySnapshotId = '00000000-0000-4000-8000-000000000000';
const CapacityRequestId = 'ffffffff-ffff-4fff-bfff-ffffffffffff';
const CapacityServerTime = '9999-12-31T23:59:59.999Z';
const CapacityStateVersion = Number.MAX_SAFE_INTEGER;

interface DeveloperRow extends Record<string, string> {
  developer_id: string;
  display_name: string;
  token_digest: string;
  token_lookup: string;
}

interface SessionRow extends Record<string, string> {
  session_id: string;
  developer_id: string;
  display_name: string;
  token_digest: string;
  token_lookup: string;
  expires_at: string;
}

interface ConnectionRow extends Record<string, string> {
  connection_id: string;
  session_id: string;
  developer_id: string;
  display_name: string;
  expires_at: string;
}

interface PresenceRow extends Record<string, string> {
  canonical_path: string;
  display_path: string;
  developer_id: string;
  display_name: string;
  connection_id: string;
  branch: string;
  task: string;
  expires_at: string;
}

interface LeaseRow extends Record<string, string> {
  lease_id: string;
  canonical_path: string;
  display_path: string;
  developer_id: string;
  display_name: string;
  branch: string;
  task: string;
  connection_id: string;
  expires_at: string;
}

interface ReservationRow extends Record<string, string> {
  reservation_id: string;
  canonical_path: string;
  display_path: string;
  developer_id: string;
  display_name: string;
  branch: string;
  task: string;
  expires_at: string;
}

interface ReplayRow extends Record<string, string> {
  payload_hash: string;
  result_json: string;
}

export interface AuthenticatedSession {
  sessionId: string;
  developerId: string;
  displayName: string;
  expiresAt: string;
}

export interface StateTransition {
  requester: ServerEnvelope | ServerEnvelope[] | null;
  stateChanges: ServerEnvelope[];
  stateVersion: number;
  connectionClosures: ConnectionClosure[];
}

export interface ConnectionClosure {
  connectionId: string;
  code: 4001 | 4003;
  reason: string;
}

export class CoordinationObject {
  private readonly initialized: Promise<void>;
  private readonly sockets = new Map<WebSocket, SocketAttachment>();

  constructor(
    readonly state: DurableObjectState,
    readonly env: Env
  ) {
    this.restoreSockets();
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
      if (new URL(request.url).search !== '') {
        return badRequest();
      }
      const session = await this.authenticateSession(request);
      if (session === null) {
        return unauthorized();
      }
      if (request.headers.get('Upgrade')?.toLowerCase() !== 'websocket') {
        return badRequest();
      }
      return this.upgradeConnection(route.projectId, session);
    }

    return new Response('Not implemented', { status: 501 });
  }

  private async upgradeConnection(projectId: string, session: SessionRow): Promise<Response> {
    const opened = await this.openConnection({
      sessionId: session.session_id,
      developerId: session.developer_id,
      displayName: session.display_name,
      expiresAt: session.expires_at
    });
    const expiredSockets = this.removeConnectionSockets(opened.connectionClosures);
    this.broadcast(opened.stateChanges);
    this.closeSockets(expiredSockets);
    const ready = opened.requester;
    if (ready === null || Array.isArray(ready) || ready.type !== 'session.ready') {
      throw new Error('Opening a connection did not return session readiness.');
    }
    const snapshot = await this.currentSnapshot();

    const [client, server] = Object.values(new WebSocketPair());
    const attachment: SocketAttachment = {
      projectId,
      sessionId: session.session_id,
      developerId: session.developer_id,
      displayName: session.display_name,
      connectionId: ready.connectionId
    };
    server.serializeAttachment(attachment);
    this.state.acceptWebSocket(server);
    this.sockets.set(server, attachment);
    this.send(server, ready);
    for (const envelope of snapshot) {
      this.send(server, envelope);
    }

    return new Response(null, { status: 101, webSocket: client });
  }

  async openConnection(session: AuthenticatedSession, now = new Date()): Promise<StateTransition> {
    await this.initialized;
    const transition = this.state.storage.transactionSync(() => {
      const pruned = this.pruneExpiredInTransaction(now);
      const authenticated = this.connectionSession(session, now);
      if (authenticated === null) {
        throw new Error('The authenticated session is no longer valid.');
      }

      const connectionId = crypto.randomUUID();
      this.state.storage.sql.exec(
        `INSERT INTO connections (
          connection_id, session_id, developer_id, display_name, expires_at
        ) VALUES (?, ?, ?, ?, ?)`,
        connectionId,
        authenticated.session_id,
        authenticated.developer_id,
        authenticated.display_name,
        authenticated.expires_at
      );
      const stateVersion = this.advanceStateVersion();
      return this.prepend(pruned, {
        requester: {
          protocolVersion: ProtocolVersion,
          type: 'session.ready',
          stateVersion,
          developerId: authenticated.developer_id,
          displayName: authenticated.display_name,
          serverTime: now.toISOString(),
          connectionId,
          leaseTtlSeconds: LeaseTtlSeconds,
          reservationTtlSeconds: ReservationTtlSeconds
        },
        stateChanges: [],
        stateVersion,
        connectionClosures: []
      });
    });
    await this.scheduleNextAlarm();
    return transition;
  }

  async currentSnapshot(now = new Date()): Promise<ServerEnvelope[]> {
    await this.initialized;
    return this.snapshotEnvelopes(undefined, now, this.stateVersion());
  }

  async webSocketMessage(ws: WebSocket, message: string | ArrayBuffer): Promise<void> {
    const attachment = this.socketAttachment(ws);
    if (attachment === null) {
      ws.close(1008, 'Invalid connection metadata.');
      return;
    }

    const parsed = clientSuppliesStateVersion(message)
      ? { ok: false as const, error: 'invalid_envelope' }
      : parseClientEnvelope(message);
    if (!parsed.ok) {
      this.send(ws, this.messageError(parsed.error));
      return;
    }

    const transition = await this.handleMessage(attachment.connectionId, parsed.value);
    this.deliverTransition(ws, transition);
  }

  async webSocketClose(ws: WebSocket): Promise<void> {
    await this.closeSocket(ws);
  }

  async webSocketError(ws: WebSocket): Promise<void> {
    await this.closeSocket(ws);
  }

  async closeConnection(connectionId: string, now = new Date()): Promise<StateTransition> {
    await this.initialized;
    const transition = this.state.storage.transactionSync(() => {
      const pruned = this.pruneExpiredInTransaction(now);
      const connection = this.connection(connectionId);
      if (connection === null) {
        return pruned;
      }

      const presence = this.presenceForConnection(connectionId);
      const leases = this.leasesForConnection(connectionId);
      this.state.storage.sql.exec('DELETE FROM presence WHERE connection_id = ?', connectionId);
      this.state.storage.sql.exec('DELETE FROM leases WHERE connection_id = ?', connectionId);
      this.state.storage.sql.exec('DELETE FROM connections WHERE connection_id = ?', connectionId);
      const stateVersion = this.advanceStateVersion();
      const stateChanges: ServerEnvelope[] = [
        ...presence.map((row) => this.presenceRemoved(row, stateVersion)),
        ...leases.flatMap((row) => this.leaseReleasedChanges(row, stateVersion))
      ];
      return this.prepend(pruned, {
        requester: null,
        stateChanges,
        stateVersion,
        connectionClosures: []
      });
    });
    await this.scheduleNextAlarm();
    return transition;
  }

  async handleMessage(
    connectionId: string,
    message: ClientEnvelope,
    now = new Date()
  ): Promise<StateTransition> {
    await this.initialized;
    const payloadHash = message.type === 'snapshot.request' ? null : await hashPayload(message);
    let transition: StateTransition;
    try {
      transition = this.state.storage.transactionSync(() => {
        const pruned = this.pruneExpiredInTransaction(now);
        const connection = this.connection(connectionId);
        if (connection === null) {
          return this.prepend(pruned, this.error(message.requestId, 'connection_not_found', now));
        }

        if (message.type === 'snapshot.request') {
          return this.prepend(pruned, this.snapshot(message.requestId, now));
        }

        const replay = this.replay(connection.developer_id, message.requestId);
        if (replay !== null) {
          if (replay.payload_hash !== payloadHash) {
            return this.prepend(pruned, this.error(message.requestId, 'replay_payload_mismatch', now));
          }
          const replayed = JSON.parse(replay.result_json) as StateTransition;
          return {
            requester: replayed.requester,
            stateChanges: pruned.stateChanges,
            stateVersion: pruned.stateVersion,
            connectionClosures: pruned.connectionClosures
          };
        }

        const beforeBytes = this.snapshotCapacityBytes();
        const applied = this.applyMessage(connection, message, now);
        if (!transitionEnvelopesFit(applied)) {
          throw new StateCapacityExceededError();
        }
        const afterBytes = this.snapshotCapacityBytes();
        if (afterBytes > MaximumSnapshotDataBytes && afterBytes > beforeBytes) {
          throw new StateCapacityExceededError();
        }
        this.storeReplay(connection.developer_id, message.requestId, payloadHash as string, applied, now);
        return this.prepend(pruned, applied);
      });
    } catch (error) {
      if (!(error instanceof StateCapacityExceededError) || message.type === 'snapshot.request') {
        throw error;
      }
      transition = this.state.storage.transactionSync(() => {
        const pruned = this.pruneExpiredInTransaction(now);
        const connection = this.connection(connectionId);
        if (connection === null) {
          return this.prepend(pruned, this.error(message.requestId, 'connection_not_found', now));
        }
        const rejected = this.error(message.requestId, 'state_capacity_exceeded', now);
        this.storeReplay(
          connection.developer_id,
          message.requestId,
          payloadHash as string,
          rejected,
          now
        );
        return this.prepend(pruned, rejected);
      });
    }
    await this.scheduleNextAlarm();
    return transition;
  }

  async pruneExpired(now = new Date()): Promise<StateTransition> {
    await this.initialized;
    const transition = this.state.storage.transactionSync(() => this.pruneExpiredInTransaction(now));
    await this.scheduleNextAlarm();
    return transition;
  }

  private pruneExpiredInTransaction(now: Date): StateTransition {
    const cutoff = now.toISOString();
    const expiredConnections = this.state.storage.sql.exec<ConnectionRow>(
      'SELECT connection_id, session_id, developer_id, display_name, expires_at FROM connections WHERE expires_at <= ?',
      cutoff
    ).toArray();
    const connectionIds = expiredConnections.map(({ connection_id }) => connection_id);
    const expiredPresence = this.expiredPresence(cutoff, connectionIds);
    const expiredLeases = this.expiredLeases(cutoff, connectionIds);
    const expiredReservations = this.state.storage.sql.exec<ReservationRow>(`
      SELECT reservation_id, canonical_path, display_path, developer_id, display_name, branch, task, expires_at
      FROM reservations WHERE expires_at <= ?
    `, cutoff).toArray();
    const expiredSessions = this.state.storage.sql.exec<{ session_id: string }>(
      'SELECT session_id FROM sessions WHERE expires_at <= ?',
      cutoff
    ).toArray();
    const expiredReplay = this.state.storage.sql.exec<{ request_id: string }>(
      'SELECT request_id FROM replay_records WHERE expires_at <= ?',
      cutoff
    ).toArray();

    if (
      expiredConnections.length === 0
      && expiredPresence.length === 0
      && expiredLeases.length === 0
      && expiredReservations.length === 0
      && expiredSessions.length === 0
      && expiredReplay.length === 0
    ) {
      return this.emptyTransition();
    }

    this.state.storage.sql.exec('DELETE FROM sessions WHERE expires_at <= ?', cutoff);
    this.state.storage.sql.exec('DELETE FROM connections WHERE expires_at <= ?', cutoff);
    this.state.storage.sql.exec('DELETE FROM presence WHERE expires_at <= ?', cutoff);
    this.state.storage.sql.exec('DELETE FROM leases WHERE expires_at <= ?', cutoff);
    if (connectionIds.length > 0) {
      const placeholders = connectionIds.map(() => '?').join(', ');
      this.state.storage.sql.exec(`DELETE FROM presence WHERE connection_id IN (${placeholders})`, ...connectionIds);
      this.state.storage.sql.exec(`DELETE FROM leases WHERE connection_id IN (${placeholders})`, ...connectionIds);
    }
    this.state.storage.sql.exec('DELETE FROM reservations WHERE expires_at <= ?', cutoff);
    this.state.storage.sql.exec('DELETE FROM replay_records WHERE expires_at <= ?', cutoff);

    const stateVersion = this.advanceStateVersion();
    const stateChanges: ServerEnvelope[] = [
      ...expiredPresence.map((row) => this.presenceRemoved(row, stateVersion)),
      ...expiredLeases.flatMap((row) => this.leaseReleasedChanges(row, stateVersion)),
      ...expiredReservations.flatMap((row) => this.reservationReleasedChanges(row, stateVersion))
    ];
    return {
      requester: null,
      stateChanges,
      stateVersion,
      connectionClosures: expiredConnections.map(({ connection_id }) => ({
        connectionId: connection_id,
        code: 4001,
        reason: 'Session expired.'
      }))
    };
  }

  async alarm(): Promise<void> {
    await this.initialized;
    const transition = await this.pruneExpired(new Date());
    const expiredSockets = this.removeConnectionSockets(transition.connectionClosures);
    this.broadcast(transition.stateChanges);
    this.closeSockets(expiredSockets);
  }

  private restoreSockets(): void {
    for (const ws of this.state.getWebSockets()) {
      const attachment = parseSocketAttachment(ws.deserializeAttachment());
      if (attachment === null) {
        ws.close(1008, 'Invalid connection metadata.');
        continue;
      }
      this.sockets.set(ws, attachment);
    }
  }

  private socketAttachment(ws: WebSocket): SocketAttachment | null {
    const existing = this.sockets.get(ws);
    if (existing !== undefined) {
      return existing;
    }

    const restored = parseSocketAttachment(ws.deserializeAttachment());
    if (restored !== null) {
      this.sockets.set(ws, restored);
    }
    return restored;
  }

  private async closeSocket(ws: WebSocket): Promise<void> {
    const attachment = this.socketAttachment(ws);
    this.sockets.delete(ws);
    if (attachment === null) {
      return;
    }

    const transition = await this.closeConnection(attachment.connectionId);
    const expiredSockets = this.removeConnectionSockets(transition.connectionClosures);
    this.broadcast(transition.stateChanges);
    this.closeSockets(expiredSockets);
  }

  private deliverTransition(ws: WebSocket, transition: StateTransition): void {
    const expiredSockets = this.removeConnectionSockets(transition.connectionClosures);
    const requester = transition.requester === null
      ? []
      : Array.isArray(transition.requester) ? transition.requester : [transition.requester];
    for (const envelope of requester) {
      if (!transition.stateChanges.includes(envelope) && this.sockets.has(ws)) {
        this.send(ws, envelope);
      }
    }
    this.broadcast(transition.stateChanges);
    this.closeSockets(expiredSockets);
  }

  private removeConnectionSockets(closures: ConnectionClosure[]): SocketClosure[] {
    const sockets: SocketClosure[] = [];
    for (const closure of closures) {
      for (const [ws, attachment] of this.sockets.entries()) {
        if (attachment.connectionId !== closure.connectionId) {
          continue;
        }
        this.sockets.delete(ws);
        sockets.push({ ws, closure });
      }
    }
    return sockets;
  }

  private closeSockets(sockets: SocketClosure[]): void {
    for (const { ws, closure } of sockets) {
      try {
        ws.close(closure.code, closure.reason);
      } catch {
        // Closing an already-closed socket does not affect committed state.
      }
    }
  }

  private broadcast(envelopes: ServerEnvelope[]): void {
    for (const envelope of envelopes) {
      for (const ws of this.sockets.keys()) {
        this.send(ws, envelope);
      }
    }
  }

  private send(ws: WebSocket, envelope: ServerEnvelope): void {
    try {
      const json = JSON.stringify(envelope);
      if (utf8Bytes(json) > MaximumEnvelopeBytes) {
        this.sockets.delete(ws);
        ws.close(1011, 'Server envelope exceeds the protocol limit.');
        return;
      }
      ws.send(json);
    } catch {
      this.sockets.delete(ws);
    }
  }

  private messageError(code: string): ServerEnvelope {
    return {
      protocolVersion: ProtocolVersion,
      type: 'error',
      stateVersion: this.stateVersion(),
      code,
      message: 'The coordination message is invalid.'
    };
  }

  private async initialize(): Promise<void> {
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS developers (
        developer_id TEXT PRIMARY KEY,
        display_name TEXT NOT NULL,
        token_digest TEXT NOT NULL UNIQUE,
        token_lookup TEXT NOT NULL UNIQUE,
        revoked_at TEXT
      )
    `);
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS sessions (
        session_id TEXT PRIMARY KEY,
        developer_id TEXT NOT NULL,
        token_digest TEXT NOT NULL UNIQUE,
        token_lookup TEXT NOT NULL UNIQUE,
        created_at TEXT NOT NULL,
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
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS connections (
        connection_id TEXT PRIMARY KEY,
        session_id TEXT NOT NULL,
        developer_id TEXT NOT NULL,
        display_name TEXT NOT NULL,
        expires_at TEXT NOT NULL
      )
    `);
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS presence (
        canonical_path TEXT NOT NULL,
        display_path TEXT NOT NULL,
        developer_id TEXT NOT NULL,
        display_name TEXT NOT NULL,
        connection_id TEXT NOT NULL,
        branch TEXT NOT NULL,
        task TEXT NOT NULL,
        expires_at TEXT NOT NULL,
        PRIMARY KEY (canonical_path, connection_id)
      )
    `);
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS leases (
        lease_id TEXT PRIMARY KEY,
        canonical_path TEXT NOT NULL UNIQUE,
        display_path TEXT NOT NULL,
        developer_id TEXT NOT NULL,
        display_name TEXT NOT NULL,
        branch TEXT NOT NULL,
        task TEXT NOT NULL,
        connection_id TEXT NOT NULL,
        created_at TEXT NOT NULL,
        expires_at TEXT NOT NULL
      )
    `);
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS reservations (
        reservation_id TEXT PRIMARY KEY,
        canonical_path TEXT NOT NULL UNIQUE,
        display_path TEXT NOT NULL,
        developer_id TEXT NOT NULL,
        display_name TEXT NOT NULL,
        branch TEXT NOT NULL,
        task TEXT NOT NULL,
        created_at TEXT NOT NULL,
        expires_at TEXT NOT NULL
      )
    `);
    this.state.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS replay_records (
        developer_id TEXT NOT NULL,
        request_id TEXT NOT NULL,
        payload_hash TEXT NOT NULL,
        result_json TEXT NOT NULL,
        expires_at TEXT NOT NULL,
        PRIMARY KEY (developer_id, request_id)
      )
    `);
    await this.scheduleNextAlarm();
  }

  private applyMessage(
    connection: ConnectionRow,
    message: Exclude<ClientEnvelope, { type: 'snapshot.request' }>,
    now: Date
  ): StateTransition {
    if (message.type === 'heartbeat') {
      return this.heartbeat(connection, message.requestId, now);
    }

    const path = pathDetails(message.path);
    if (path === null) {
      return this.error(message.requestId, 'invalid_path', now);
    }
    if (message.type === 'presence.open') {
      return this.openPresence(connection, message, path, now);
    }
    if (message.type === 'presence.close') {
      return this.closePresence(connection, message.requestId, path, now);
    }
    if (message.type === 'lease.acquire') {
      return this.acquireLease(connection, message, path, now);
    }
    if (message.type === 'lease.release') {
      return this.releaseLease(connection, message.requestId, path, now);
    }
    if (message.type === 'lease.reserve') {
      return this.reserveLease(connection, message, path, now);
    }
    if (message.type === 'reservation.cancel') {
      return this.cancelReservation(connection, message.requestId, path, now);
    }
    return this.overrideLease(connection, message, path, now);
  }

  private openPresence(
    connection: ConnectionRow,
    message: Extract<ClientEnvelope, { type: 'presence.open' }>,
    path: PathDetails,
    now: Date
  ): StateTransition {
    const expiresAt = expiry(now, LeaseTtlSeconds);
    this.state.storage.sql.exec(`
      INSERT INTO presence (
        canonical_path, display_path, developer_id, display_name, connection_id, branch, task, expires_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
      ON CONFLICT(canonical_path, connection_id) DO UPDATE SET
        display_path = excluded.display_path,
        developer_id = excluded.developer_id,
        display_name = excluded.display_name,
        branch = excluded.branch,
        task = excluded.task,
        expires_at = excluded.expires_at
    `,
    path.canonical,
    path.display,
    connection.developer_id,
    connection.display_name,
    connection.connection_id,
    message.branch,
    message.task,
    expiresAt);
    const stateVersion = this.advanceStateVersion();
    const event: ServerEnvelope = {
      protocolVersion: ProtocolVersion,
      type: 'presence.updated',
      stateVersion,
      requestId: message.requestId,
      presence: this.presenceForPath(path.canonical)
    };
    return { requester: event, stateChanges: [event], stateVersion, connectionClosures: [] };
  }

  private closePresence(
    connection: ConnectionRow,
    requestId: string,
    path: PathDetails,
    now: Date
  ): StateTransition {
    const presence = first(this.state.storage.sql.exec<PresenceRow>(`
      SELECT canonical_path, display_path, developer_id, display_name, connection_id, branch, task, expires_at
      FROM presence WHERE canonical_path = ? AND connection_id = ?
    `, path.canonical, connection.connection_id));
    if (presence === null) {
      return this.error(requestId, 'presence_not_found', now);
    }
    this.state.storage.sql.exec(
      'DELETE FROM presence WHERE canonical_path = ? AND connection_id = ?',
      path.canonical,
      connection.connection_id
    );
    const stateVersion = this.advanceStateVersion();
    const event = this.presenceRemoved(presence, stateVersion, requestId);
    return { requester: event, stateChanges: [event], stateVersion, connectionClosures: [] };
  }

  private acquireLease(
    connection: ConnectionRow,
    message: Extract<ClientEnvelope, { type: 'lease.acquire' }>,
    path: PathDetails,
    now: Date
  ): StateTransition {
    const existingLease = this.lease(path.canonical);
    const reservation = this.reservation(path.canonical);
    if (existingLease !== null && existingLease.developer_id !== connection.developer_id) {
      return this.denied(
        message.requestId,
        path.canonical,
        'lease_unavailable',
        this.leaseRecord(existingLease),
        now
      );
    }
    if (reservation !== null && reservation.developer_id !== connection.developer_id) {
      return this.denied(
        message.requestId,
        path.canonical,
        'lease_unavailable',
        this.reservationRecord(reservation),
        now
      );
    }

    const expiresAt = expiry(now, LeaseTtlSeconds);
    const leaseId = existingLease?.lease_id ?? crypto.randomUUID();
    if (existingLease === null) {
      this.state.storage.sql.exec(`
        INSERT INTO leases (
          lease_id, canonical_path, display_path, developer_id, display_name, branch, task,
          connection_id, created_at, expires_at
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
      `,
      leaseId,
      path.canonical,
      path.display,
      connection.developer_id,
      connection.display_name,
      message.branch,
      message.task,
      connection.connection_id,
      now.toISOString(),
      expiresAt);
    } else {
      this.state.storage.sql.exec(`
        UPDATE leases SET display_path = ?, display_name = ?, branch = ?, task = ?,
          connection_id = ?, expires_at = ? WHERE lease_id = ?
      `,
      path.display,
      connection.display_name,
      message.branch,
      message.task,
      connection.connection_id,
      expiresAt,
      existingLease.lease_id);
    }

    const stateVersion = this.advanceStateVersion();
    const lease = this.leaseRecord(this.lease(path.canonical) as LeaseRow);
    const event: ServerEnvelope = {
      protocolVersion: ProtocolVersion,
      type: 'lease.granted',
      stateVersion,
      requestId: message.requestId,
      path: path.canonical,
      lease
    };
    return { requester: event, stateChanges: [event], stateVersion, connectionClosures: [] };
  }

  private releaseLease(
    connection: ConnectionRow,
    requestId: string,
    path: PathDetails,
    now: Date
  ): StateTransition {
    const lease = this.lease(path.canonical);
    if (
      lease === null
      || lease.developer_id !== connection.developer_id
      || lease.connection_id !== connection.connection_id
    ) {
      return this.denied(requestId, path.canonical, 'lease_not_owned', this.effectiveLease(path.canonical), now);
    }
    this.state.storage.sql.exec('DELETE FROM leases WHERE lease_id = ?', lease.lease_id);
    const stateVersion = this.advanceStateVersion();
    const changes = this.leaseReleasedChanges(lease, stateVersion, requestId);
    return { requester: changes[0], stateChanges: changes, stateVersion, connectionClosures: [] };
  }

  private reserveLease(
    connection: ConnectionRow,
    message: Extract<ClientEnvelope, { type: 'lease.reserve' }>,
    path: PathDetails,
    now: Date
  ): StateTransition {
    const current = this.effectiveLease(path.canonical);
    if (current !== null) {
      return this.denied(message.requestId, path.canonical, 'lease_unavailable', current, now);
    }

    const reservationId = crypto.randomUUID();
    const expiresAt = expiry(now, ReservationTtlSeconds);
    this.state.storage.sql.exec(`
      INSERT INTO reservations (
        reservation_id, canonical_path, display_path, developer_id, display_name, branch, task,
        created_at, expires_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    `,
    reservationId,
    path.canonical,
    path.display,
    connection.developer_id,
    connection.display_name,
    message.branch,
    message.task,
    now.toISOString(),
    expiresAt);
    const stateVersion = this.advanceStateVersion();
    const lease = this.reservationRecord(this.reservation(path.canonical) as ReservationRow);
    const event: ServerEnvelope = {
      protocolVersion: ProtocolVersion,
      type: 'lease.granted',
      stateVersion,
      requestId: message.requestId,
      path: path.canonical,
      lease
    };
    return { requester: event, stateChanges: [event], stateVersion, connectionClosures: [] };
  }

  private cancelReservation(
    connection: ConnectionRow,
    requestId: string,
    path: PathDetails,
    now: Date
  ): StateTransition {
    const reservation = this.reservation(path.canonical);
    if (reservation === null || reservation.developer_id !== connection.developer_id) {
      return this.denied(
        requestId,
        path.canonical,
        'reservation_not_owned',
        this.effectiveLease(path.canonical),
        now
      );
    }
    this.state.storage.sql.exec(
      'DELETE FROM reservations WHERE reservation_id = ?',
      reservation.reservation_id
    );
    const stateVersion = this.advanceStateVersion();
    const changes = this.reservationReleasedChanges(reservation, stateVersion, requestId);
    return { requester: changes[0], stateChanges: changes, stateVersion, connectionClosures: [] };
  }

  private overrideLease(
    connection: ConnectionRow,
    message: Extract<ClientEnvelope, { type: 'lease.override' }>,
    path: PathDetails,
    now: Date
  ): StateTransition {
    const current = this.effectiveLease(path.canonical);
    if (current === null) {
      return this.denied(message.requestId, path.canonical, 'lease_unavailable', null, now);
    }
    if (current.developerId === connection.developer_id) {
      return this.denied(message.requestId, path.canonical, 'lease_already_owned', current, now);
    }

    this.state.storage.sql.exec('DELETE FROM leases WHERE canonical_path = ?', path.canonical);
    this.state.storage.sql.exec('DELETE FROM reservations WHERE canonical_path = ?', path.canonical);
    const leaseId = crypto.randomUUID();
    this.state.storage.sql.exec(`
      INSERT INTO leases (
        lease_id, canonical_path, display_path, developer_id, display_name, branch, task,
        connection_id, created_at, expires_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `,
    leaseId,
    path.canonical,
    path.display,
    connection.developer_id,
    connection.display_name,
    message.branch,
    message.task,
    connection.connection_id,
    now.toISOString(),
    expiry(now, LeaseTtlSeconds));
    const stateVersion = this.advanceStateVersion();
    const lease = this.leaseRecord(this.lease(path.canonical) as LeaseRow);
    const event: ServerEnvelope = {
      protocolVersion: ProtocolVersion,
      type: 'lease.overridden',
      stateVersion,
      requestId: message.requestId,
      path: path.canonical,
      previousDeveloperId: current.developerId,
      lease
    };
    return { requester: event, stateChanges: [event], stateVersion, connectionClosures: [] };
  }

  private heartbeat(connection: ConnectionRow, requestId: string, now: Date): StateTransition {
    const presence = this.presenceForConnection(connection.connection_id);
    const leases = this.leasesForConnection(connection.connection_id);
    if (presence.length === 0 && leases.length === 0) {
      return this.snapshot(requestId, now);
    }

    const expiresAt = expiry(now, LeaseTtlSeconds);
    this.state.storage.sql.exec('UPDATE presence SET expires_at = ? WHERE connection_id = ?', expiresAt, connection.connection_id);
    this.state.storage.sql.exec('UPDATE leases SET expires_at = ? WHERE connection_id = ?', expiresAt, connection.connection_id);
    const stateVersion = this.advanceStateVersion();
    const stateChanges: ServerEnvelope[] = [
      ...Array.from(new Set(presence.map(({ canonical_path }) => canonical_path))).map((canonicalPath) => ({
        protocolVersion: ProtocolVersion,
        type: 'presence.updated' as const,
        stateVersion,
        presence: this.presenceForPath(canonicalPath)
      })),
      ...leases.map((lease) => ({
        protocolVersion: ProtocolVersion,
        type: 'lease.updated' as const,
        stateVersion,
        lease: this.leaseRecord(this.lease(lease.canonical_path) as LeaseRow)
      }))
    ];
    return {
      requester: this.snapshotEnvelopes(requestId, now, stateVersion),
      stateChanges,
      stateVersion,
      connectionClosures: []
    };
  }

  private snapshot(requestId: string, now: Date): StateTransition {
    const stateVersion = this.stateVersion();
    return {
      requester: this.snapshotEnvelopes(requestId, now, stateVersion),
      stateChanges: [],
      stateVersion,
      connectionClosures: []
    };
  }

  private snapshotEnvelopes(
    requestId: string | undefined,
    now: Date,
    stateVersion: number
  ): ServerEnvelope[] {
    try {
      return buildSnapshotChunks({
        snapshotId: crypto.randomUUID(),
        stateVersion,
        requestId,
        serverTime: now.toISOString(),
        presence: this.allPresence(),
        leases: this.allEffectiveLeases()
      });
    } catch (error) {
      if (!(error instanceof SnapshotRecordTooLargeError)) {
        throw error;
      }
      return [{
        protocolVersion: ProtocolVersion,
        type: 'error',
        stateVersion,
        ...(requestId === undefined ? {} : { requestId }),
        code: 'snapshot_record_too_large',
        message: errorMessage('snapshot_record_too_large')
      }];
    }
  }

  private snapshotCapacityBytes(): number {
    try {
      return buildSnapshotChunks({
        snapshotId: CapacitySnapshotId,
        stateVersion: CapacityStateVersion,
        requestId: CapacityRequestId,
        serverTime: CapacityServerTime,
        presence: this.allPresence(),
        leases: this.allEffectiveLeases()
      }).reduce((bytes, envelope) => bytes + serializedEnvelopeBytes(envelope), 0);
    } catch (error) {
      if (error instanceof SnapshotRecordTooLargeError) {
        return Number.POSITIVE_INFINITY;
      }
      throw error;
    }
  }

  private denied(
    requestId: string,
    path: string,
    code: string,
    currentLease: LeaseRecord | null,
    now: Date
  ): StateTransition {
    const stateVersion = this.stateVersion();
    return {
      requester: {
        protocolVersion: ProtocolVersion,
        type: 'lease.denied',
        stateVersion,
        requestId,
        path,
        code,
        currentLease
      },
      stateChanges: [],
      stateVersion,
      connectionClosures: []
    };
  }

  private error(requestId: string, code: string, _now: Date): StateTransition {
    const stateVersion = this.stateVersion();
    return {
      requester: {
        protocolVersion: ProtocolVersion,
        type: 'error',
        stateVersion,
        requestId,
        code,
        message: errorMessage(code)
      },
      stateChanges: [],
      stateVersion,
      connectionClosures: []
    };
  }

  private presenceRemoved(row: PresenceRow, stateVersion: number, requestId?: string): ServerEnvelope {
    return {
      protocolVersion: ProtocolVersion,
      type: 'presence.removed',
      stateVersion,
      ...(requestId === undefined ? {} : { requestId }),
      path: row.canonical_path,
      connectionId: row.connection_id
    };
  }

  private leaseReleasedChanges(
    row: LeaseRow,
    stateVersion: number,
    requestId?: string
  ): ServerEnvelope[] {
    const released: ServerEnvelope = {
      protocolVersion: ProtocolVersion,
      type: 'lease.released',
      stateVersion,
      ...(requestId === undefined ? {} : { requestId }),
      path: row.canonical_path,
      leaseId: row.lease_id
    };
    const reservation = this.reservation(row.canonical_path);
    if (reservation === null) {
      return [released];
    }
    return [released, {
      protocolVersion: ProtocolVersion,
      type: 'lease.updated',
      stateVersion,
      lease: this.reservationRecord(reservation)
    }];
  }

  private reservationReleasedChanges(
    row: ReservationRow,
    stateVersion: number,
    requestId?: string
  ): ServerEnvelope[] {
    return [{
      protocolVersion: ProtocolVersion,
      type: 'lease.released',
      stateVersion,
      ...(requestId === undefined ? {} : { requestId }),
      path: row.canonical_path,
      leaseId: row.reservation_id
    }];
  }

  private connectionSession(session: AuthenticatedSession, now: Date): SessionRow | null {
    const row = first(this.state.storage.sql.exec<SessionRow>(`
      SELECT sessions.session_id, sessions.developer_id, developers.display_name,
        sessions.token_digest, sessions.expires_at
      FROM sessions INNER JOIN developers ON developers.developer_id = sessions.developer_id
      WHERE sessions.session_id = ? AND sessions.developer_id = ?
        AND developers.revoked_at IS NULL
    `, session.sessionId, session.developerId));
    if (
      row === null
      || row.display_name !== session.displayName
      || row.expires_at !== session.expiresAt
      || isExpired(row.expires_at, now)
    ) {
      return null;
    }
    return row;
  }

  private connection(connectionId: string): ConnectionRow | null {
    return first(this.state.storage.sql.exec<ConnectionRow>(`
      SELECT connection_id, session_id, developer_id, display_name, expires_at
      FROM connections WHERE connection_id = ?
    `, connectionId));
  }

  private lease(canonicalPath: string): LeaseRow | null {
    return first(this.state.storage.sql.exec<LeaseRow>(`
      SELECT lease_id, canonical_path, display_path, developer_id, display_name, branch, task,
        connection_id, expires_at
      FROM leases WHERE canonical_path = ?
    `, canonicalPath));
  }

  private reservation(canonicalPath: string): ReservationRow | null {
    return first(this.state.storage.sql.exec<ReservationRow>(`
      SELECT reservation_id, canonical_path, display_path, developer_id, display_name, branch, task, expires_at
      FROM reservations WHERE canonical_path = ?
    `, canonicalPath));
  }

  private effectiveLease(canonicalPath: string): LeaseRecord | null {
    const lease = this.lease(canonicalPath);
    if (lease !== null) {
      return this.leaseRecord(lease);
    }
    const reservation = this.reservation(canonicalPath);
    return reservation === null ? null : this.reservationRecord(reservation);
  }

  private allEffectiveLeases(): LeaseRecord[] {
    const leases = this.state.storage.sql.exec<LeaseRow>(`
      SELECT lease_id, canonical_path, display_path, developer_id, display_name, branch, task,
        connection_id, expires_at
      FROM leases ORDER BY canonical_path
    `).toArray().map((row) => this.leaseRecord(row));
    const leasedPaths = new Set(leases.map(({ path }) => path));
    const reservations = this.state.storage.sql.exec<ReservationRow>(`
      SELECT reservation_id, canonical_path, display_path, developer_id, display_name, branch, task, expires_at
      FROM reservations ORDER BY canonical_path
    `).toArray().map((row) => this.reservationRecord(row));
    return [...leases, ...reservations.filter(({ path }) => !leasedPaths.has(path))];
  }

  private allPresence(): PresenceRecord[] {
    return this.state.storage.sql.exec<PresenceRow>(`
      SELECT canonical_path, display_path, developer_id, display_name, connection_id, branch, task, expires_at
      FROM presence ORDER BY canonical_path, connection_id
    `).toArray().map((row) => this.presenceRecord(row));
  }

  private presenceForPath(canonicalPath: string): PresenceRecord[] {
    return this.state.storage.sql.exec<PresenceRow>(`
      SELECT canonical_path, display_path, developer_id, display_name, connection_id, branch, task, expires_at
      FROM presence WHERE canonical_path = ? ORDER BY connection_id
    `, canonicalPath).toArray().map((row) => this.presenceRecord(row));
  }

  private presenceForConnection(connectionId: string): PresenceRow[] {
    return this.state.storage.sql.exec<PresenceRow>(`
      SELECT canonical_path, display_path, developer_id, display_name, connection_id, branch, task, expires_at
      FROM presence WHERE connection_id = ?
    `, connectionId).toArray();
  }

  private leasesForConnection(connectionId: string): LeaseRow[] {
    return this.state.storage.sql.exec<LeaseRow>(`
      SELECT lease_id, canonical_path, display_path, developer_id, display_name, branch, task,
        connection_id, expires_at
      FROM leases WHERE connection_id = ?
    `, connectionId).toArray();
  }

  private expiredPresence(cutoff: string, connectionIds: string[]): PresenceRow[] {
    const base = `
      SELECT canonical_path, display_path, developer_id, display_name, connection_id, branch, task, expires_at
      FROM presence WHERE expires_at <= ?`;
    if (connectionIds.length === 0) {
      return this.state.storage.sql.exec<PresenceRow>(base, cutoff).toArray();
    }
    const placeholders = connectionIds.map(() => '?').join(', ');
    return this.state.storage.sql.exec<PresenceRow>(
      `${base} OR connection_id IN (${placeholders})`,
      cutoff,
      ...connectionIds
    ).toArray();
  }

  private expiredLeases(cutoff: string, connectionIds: string[]): LeaseRow[] {
    const base = `
      SELECT lease_id, canonical_path, display_path, developer_id, display_name, branch, task,
        connection_id, expires_at FROM leases WHERE expires_at <= ?`;
    if (connectionIds.length === 0) {
      return this.state.storage.sql.exec<LeaseRow>(base, cutoff).toArray();
    }
    const placeholders = connectionIds.map(() => '?').join(', ');
    return this.state.storage.sql.exec<LeaseRow>(
      `${base} OR connection_id IN (${placeholders})`,
      cutoff,
      ...connectionIds
    ).toArray();
  }

  private replay(developerId: string, requestId: string): ReplayRow | null {
    return first(this.state.storage.sql.exec<ReplayRow>(`
      SELECT payload_hash, result_json FROM replay_records
      WHERE developer_id = ? AND request_id = ?
    `, developerId, requestId));
  }

  private storeReplay(
    developerId: string,
    requestId: string,
    payloadHash: string,
    result: StateTransition,
    now: Date
  ): void {
    this.state.storage.sql.exec(`
      INSERT INTO replay_records (developer_id, request_id, payload_hash, result_json, expires_at)
      VALUES (?, ?, ?, ?, ?)
    `,
    developerId,
    requestId,
    payloadHash,
    JSON.stringify(result),
    new Date(now.getTime() + ReplayTtlMilliseconds).toISOString());
  }

  private presenceRecord(row: PresenceRow): PresenceRecord {
    return {
      path: row.canonical_path,
      displayPath: row.display_path,
      developerId: row.developer_id,
      displayName: row.display_name,
      connectionId: row.connection_id,
      branch: row.branch,
      task: row.task,
      expiresAt: row.expires_at
    };
  }

  private leaseRecord(row: LeaseRow): LeaseRecord {
    return {
      leaseId: row.lease_id,
      path: row.canonical_path,
      displayPath: row.display_path,
      mode: 'editing',
      developerId: row.developer_id,
      displayName: row.display_name,
      branch: row.branch,
      task: row.task,
      expiresAt: row.expires_at,
      connectionId: row.connection_id
    };
  }

  private reservationRecord(row: ReservationRow): LeaseRecord {
    return {
      leaseId: row.reservation_id,
      path: row.canonical_path,
      displayPath: row.display_path,
      mode: 'reserved',
      developerId: row.developer_id,
      displayName: row.display_name,
      branch: row.branch,
      task: row.task,
      expiresAt: row.expires_at
    };
  }

  private advanceStateVersion(): number {
    this.state.storage.sql.exec(
      "UPDATE coordination_state SET value = value + 1 WHERE key = 'state_version'"
    );
    return this.stateVersion();
  }

  private stateVersion(): number {
    return this.state.storage.sql.exec<{ value: number }>(
      "SELECT value FROM coordination_state WHERE key = 'state_version'"
    ).one().value;
  }

  private emptyTransition(): StateTransition {
    return {
      requester: null,
      stateChanges: [],
      stateVersion: this.stateVersion(),
      connectionClosures: []
    };
  }

  private prepend(first: StateTransition, second: StateTransition): StateTransition {
    return {
      requester: second.requester,
      stateChanges: [...first.stateChanges, ...second.stateChanges],
      stateVersion: second.stateVersion,
      connectionClosures: [...first.connectionClosures, ...second.connectionClosures]
    };
  }

  private async scheduleNextAlarm(): Promise<void> {
    const expiresAt = ['sessions', 'connections', 'presence', 'leases', 'reservations', 'replay_records']
      .map((table) => this.state.storage.sql.exec<{ expires_at: string | null }>(
        `SELECT MIN(expires_at) AS expires_at FROM ${table}`
      ).one().expires_at)
      .filter((value): value is string => value !== null)
      .sort()[0];
    if (expiresAt === undefined) {
      await this.state.storage.deleteAlarm();
      return;
    }
    await this.state.storage.setAlarm(Date.parse(expiresAt));
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
    const [tokenDigest, tokenLookup] = await Promise.all([
      createDeveloperTokenDigest(
        this.env.TOKEN_HMAC_KEY,
        developerToken,
        developerId,
        body.displayName
      ),
      createTokenLookup(developerToken)
    ]);
    this.state.storage.sql.exec(
      `INSERT INTO developers (
        developer_id, display_name, token_digest, token_lookup, revoked_at
      ) VALUES (?, ?, ?, ?, NULL)`,
      developerId,
      body.displayName,
      tokenDigest,
      tokenLookup
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
    const [tokenDigest, tokenLookup] = await Promise.all([
      createSessionTokenDigest(
        this.env.TOKEN_HMAC_KEY,
        sessionToken,
        developer.developer_id,
        expiresAt
      ),
      createTokenLookup(sessionToken)
    ]);
    const created = this.state.storage.transactionSync(() => {
      const cutoff = serverTime.toISOString();
      this.state.storage.sql.exec(
        'DELETE FROM sessions WHERE developer_id = ? AND expires_at <= ?',
        developer.developer_id,
        cutoff
      );
      let sessions = this.state.storage.sql.exec<SessionRow>(`
        SELECT session_id, developer_id, token_digest, token_lookup, expires_at
        FROM sessions WHERE developer_id = ? AND expires_at > ? ORDER BY created_at, session_id
      `, developer.developer_id, cutoff).toArray();
      while (sessions.length >= 8) {
        const oldestDisconnected = first(this.state.storage.sql.exec<{ session_id: string }>(`
          SELECT sessions.session_id FROM sessions
          WHERE sessions.developer_id = ? AND sessions.expires_at > ?
            AND NOT EXISTS (
              SELECT 1 FROM connections
              WHERE connections.session_id = sessions.session_id AND connections.expires_at > ?
            )
          ORDER BY sessions.created_at, sessions.session_id LIMIT 1
        `, developer.developer_id, cutoff, cutoff));
        if (oldestDisconnected === null) {
          return false;
        }
        this.state.storage.sql.exec(
          'DELETE FROM sessions WHERE session_id = ?',
          oldestDisconnected.session_id
        );
        sessions = sessions.filter(({ session_id }) => session_id !== oldestDisconnected.session_id);
      }
      this.state.storage.sql.exec(
        `INSERT INTO sessions (
          session_id, developer_id, token_digest, token_lookup, created_at, expires_at
        ) VALUES (?, ?, ?, ?, ?, ?)`,
        crypto.randomUUID(),
        developer.developer_id,
        tokenDigest,
        tokenLookup,
        cutoff,
        expiresAt
      );
      return true;
    });
    if (!created) {
      return new Response('Too many sessions', { status: 429 });
    }
    await this.scheduleNextAlarm();

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
    const result = this.state.storage.transactionSync(() => {
      const pruned = this.pruneExpiredInTransaction(new Date(revokedAt));
      const exists = this.state.storage.sql.exec(
        'SELECT developer_id FROM developers WHERE developer_id = ?',
        developerId
      ).toArray().length > 0;
      if (!exists) {
        return { exists: false, transition: pruned };
      }

      const connections = this.state.storage.sql.exec<ConnectionRow>(`
        SELECT connection_id, session_id, developer_id, display_name, expires_at
        FROM connections WHERE developer_id = ?
      `, developerId).toArray();
      const connectionIds = connections.map(({ connection_id }) => connection_id);
      const presence = this.state.storage.sql.exec<PresenceRow>(`
        SELECT canonical_path, display_path, developer_id, display_name, connection_id, branch, task, expires_at
        FROM presence WHERE developer_id = ?
      `, developerId).toArray();
      const leases = this.state.storage.sql.exec<LeaseRow>(`
        SELECT lease_id, canonical_path, display_path, developer_id, display_name, branch, task,
          connection_id, expires_at
        FROM leases WHERE developer_id = ?
      `, developerId).toArray();
      const reservations = this.state.storage.sql.exec<ReservationRow>(`
        SELECT reservation_id, canonical_path, display_path, developer_id, display_name, branch, task,
          expires_at
        FROM reservations WHERE developer_id = ?
      `, developerId).toArray();

      this.state.storage.sql.exec(
        'UPDATE developers SET revoked_at = ? WHERE developer_id = ? AND revoked_at IS NULL',
        revokedAt,
        developerId
      );
      this.state.storage.sql.exec('DELETE FROM sessions WHERE developer_id = ?', developerId);
      this.state.storage.sql.exec('DELETE FROM presence WHERE developer_id = ?', developerId);
      this.state.storage.sql.exec('DELETE FROM leases WHERE developer_id = ?', developerId);
      this.state.storage.sql.exec('DELETE FROM reservations WHERE developer_id = ?', developerId);
      this.state.storage.sql.exec('DELETE FROM connections WHERE developer_id = ?', developerId);
      const claimsChanged = presence.length > 0 || leases.length > 0 || reservations.length > 0;
      const stateVersion = claimsChanged
        ? this.advanceStateVersion()
        : this.stateVersion();
      const stateChanges = claimsChanged
        ? [
          ...presence.map((row) => this.presenceRemoved(row, stateVersion)),
          ...leases.flatMap((row) => this.leaseReleasedChanges(row, stateVersion)),
          ...reservations.flatMap((row) => this.reservationReleasedChanges(row, stateVersion))
        ]
        : [];
      const transition: StateTransition = {
        requester: null,
        stateChanges,
        stateVersion,
        connectionClosures: connectionIds.map((connectionId) => ({
          connectionId,
          code: 4003,
          reason: 'Developer access revoked.'
        }))
      };
      return { exists: true, transition: this.prepend(pruned, transition) };
    });
    await this.scheduleNextAlarm();
    const sockets = this.removeConnectionSockets(result.transition.connectionClosures);
    this.broadcast(result.transition.stateChanges);
    this.closeSockets(sockets);
    if (!result.exists) {
      return new Response('Not found', { status: 404 });
    }
    return new Response(null, { status: 204 });
  }

  private async authenticateDeveloper(request: Request): Promise<DeveloperRow | null> {
    const token = readBearerToken(request);
    if (token === null) {
      return null;
    }

    const tokenLookup = await createTokenLookup(token);
    const developer = first(this.state.storage.sql.exec<DeveloperRow>(`
      SELECT developer_id, display_name, token_digest, token_lookup
      FROM developers WHERE token_lookup = ? AND revoked_at IS NULL LIMIT 1
    `, tokenLookup));
    if (developer === null) {
      return null;
    }

    const digest = await createDeveloperTokenDigest(
      this.env.TOKEN_HMAC_KEY,
      token,
      developer.developer_id,
      developer.display_name
    );
    return constantTimeEquals(digest, developer.token_digest) ? developer : null;
  }

  private async authenticateSession(request: Request): Promise<SessionRow | null> {
    const token = readBearerToken(request);
    if (token === null) {
      return null;
    }

    const serverTime = new Date();
    const tokenLookup = await createTokenLookup(token);
    const session = first(this.state.storage.sql.exec<SessionRow>(`
      SELECT sessions.session_id, sessions.developer_id, developers.display_name,
        sessions.token_digest, sessions.token_lookup, sessions.expires_at
      FROM sessions INNER JOIN developers ON developers.developer_id = sessions.developer_id
      WHERE sessions.token_lookup = ? AND developers.revoked_at IS NULL
        AND sessions.expires_at > ? LIMIT 1
    `, tokenLookup, serverTime.toISOString()));
    if (session === null) {
      return null;
    }

    const digest = await createSessionTokenDigest(
      this.env.TOKEN_HMAC_KEY,
      token,
      session.developer_id,
      session.expires_at
    );
    return constantTimeEquals(digest, session.token_digest) ? session : null;
  }
}

interface SnapshotSource {
  snapshotId: string;
  stateVersion: number;
  requestId: string | undefined;
  serverTime: string;
  presence: PresenceRecord[];
  leases: LeaseRecord[];
}

interface SnapshotChunkData {
  presence: PresenceRecord[];
  leases: LeaseRecord[];
}

type SnapshotItem =
  | { kind: 'presence'; value: PresenceRecord }
  | { kind: 'lease'; value: LeaseRecord };

class StateCapacityExceededError extends Error {}

class SnapshotRecordTooLargeError extends Error {}

function buildSnapshotChunks(source: SnapshotSource): ServerEnvelope[] {
  const items: SnapshotItem[] = [
    ...source.presence.map((value): SnapshotItem => ({ kind: 'presence', value })),
    ...source.leases.map((value): SnapshotItem => ({ kind: 'lease', value }))
  ];
  let chunkCount = 1;
  for (let attempt = 0; attempt <= items.length + 2; attempt += 1) {
    const chunks = packSnapshotItems(source, items, chunkCount);
    if (chunks.length === chunkCount) {
      return chunks.map((chunk, chunkIndex) => snapshotChunk(
        source,
        chunk,
        chunkIndex,
        chunkCount
      ));
    }
    chunkCount = chunks.length;
  }
  throw new Error('Snapshot chunk count did not converge.');
}

function packSnapshotItems(
  source: SnapshotSource,
  items: SnapshotItem[],
  chunkCount: number
): SnapshotChunkData[] {
  const chunks: SnapshotChunkData[] = [];
  let current: SnapshotChunkData = { presence: [], leases: [] };
  for (const item of items) {
    const candidate: SnapshotChunkData = item.kind === 'presence'
      ? { presence: [...current.presence, item.value], leases: current.leases }
      : { presence: current.presence, leases: [...current.leases, item.value] };
    if (snapshotChunkBytes(source, candidate, chunks.length, chunkCount) <= MaximumEnvelopeBytes) {
      current = candidate;
      continue;
    }
    if (current.presence.length === 0 && current.leases.length === 0) {
      throw new SnapshotRecordTooLargeError();
    }
    chunks.push(current);
    current = item.kind === 'presence'
      ? { presence: [item.value], leases: [] }
      : { presence: [], leases: [item.value] };
    if (snapshotChunkBytes(source, current, chunks.length, chunkCount) > MaximumEnvelopeBytes) {
      throw new SnapshotRecordTooLargeError();
    }
  }
  chunks.push(current);
  return chunks;
}

function snapshotChunk(
  source: SnapshotSource,
  data: SnapshotChunkData,
  chunkIndex: number,
  chunkCount: number
): Extract<ServerEnvelope, { type: 'snapshot' }> {
  return {
    protocolVersion: ProtocolVersion,
    type: 'snapshot',
    stateVersion: source.stateVersion,
    ...(source.requestId === undefined ? {} : { requestId: source.requestId }),
    snapshotId: source.snapshotId,
    chunkIndex,
    chunkCount,
    presence: data.presence,
    leases: data.leases,
    serverTime: source.serverTime
  };
}

function snapshotChunkBytes(
  source: SnapshotSource,
  data: SnapshotChunkData,
  chunkIndex: number,
  chunkCount: number
): number {
  return serializedEnvelopeBytes(snapshotChunk(source, data, chunkIndex, chunkCount));
}

function transitionEnvelopesFit(transition: StateTransition): boolean {
  const requester = transition.requester === null
    ? []
    : Array.isArray(transition.requester) ? transition.requester : [transition.requester];
  return [...requester, ...transition.stateChanges].every((envelope) => {
    return serializedEnvelopeBytes(envelope) <= MaximumEnvelopeBytes;
  });
}

function serializedEnvelopeBytes(envelope: ServerEnvelope): number {
  return utf8Bytes(JSON.stringify(envelope));
}

interface PathDetails {
  canonical: string;
  display: string;
}

interface SocketAttachment {
  projectId: string;
  sessionId: string;
  developerId: string;
  displayName: string;
  connectionId: string;
}

interface SocketClosure {
  ws: WebSocket;
  closure: ConnectionClosure;
}

function parseSocketAttachment(value: unknown): SocketAttachment | null {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    return null;
  }

  const attachment = value as Record<string, unknown>;
  const fields = ['projectId', 'sessionId', 'developerId', 'displayName', 'connectionId'];
  return fields.every((field) => typeof attachment[field] === 'string' && attachment[field].length > 0)
    ? attachment as unknown as SocketAttachment
    : null;
}

function clientSuppliesStateVersion(message: string | ArrayBuffer): boolean {
  if (typeof message !== 'string') {
    return false;
  }

  try {
    const value: unknown = JSON.parse(message);
    return typeof value === 'object' && value !== null && !Array.isArray(value)
      && 'stateVersion' in value;
  } catch {
    return false;
  }
}

type Route =
  | { kind: 'developers'; projectId: string }
  | { kind: 'developer'; projectId: string; developerId: string }
  | { kind: 'sessions'; projectId: string }
  | { kind: 'connect'; projectId: string };

function parseRoute(pathname: string): Route | null {
  const match = /^\/v1\/projects\/([^/]+)\/(developers|sessions|connect)(?:\/([^/]+))?$/.exec(pathname);
  if (match === null) {
    return null;
  }

  let projectId: string;
  try {
    projectId = decodeURIComponent(match[1]);
  } catch {
    return null;
  }
  if (projectId.length === 0) {
    return null;
  }

  if (match[2] === 'developers') {
    return match[3] === undefined
      ? { kind: 'developers', projectId }
      : { kind: 'developer', projectId, developerId: match[3] };
  }
  if (match[3] !== undefined) {
    return null;
  }
  return match[2] === 'sessions' ? { kind: 'sessions', projectId } : { kind: 'connect', projectId };
}

function pathDetails(path: string): PathDetails | null {
  const display = normalizePath(path);
  const canonical = canonicalPathKey(path);
  return display === null || canonical === null ? null : { canonical, display };
}

function expiry(now: Date, seconds: number): string {
  return new Date(now.getTime() + seconds * 1000).toISOString();
}

function errorMessage(code: string): string {
  switch (code) {
    case 'connection_not_found':
      return 'The connection is no longer active.';
    case 'replay_payload_mismatch':
      return 'The request identifier was reused with a different payload.';
    case 'presence_not_found':
      return 'The connection has no presence for this path.';
    case 'invalid_path':
      return 'The path is invalid.';
    case 'state_capacity_exceeded':
      return 'The project coordination state exceeds its capacity.';
    case 'snapshot_record_too_large':
      return 'A snapshot record exceeds the protocol envelope limit.';
    default:
      return 'The request could not be completed.';
  }
}

async function hashPayload(message: ClientEnvelope): Promise<string> {
  const bytes = new TextEncoder().encode(stableJson(message));
  const digest = await crypto.subtle.digest('SHA-256', bytes);
  return Array.from(new Uint8Array(digest), (value) => value.toString(16).padStart(2, '0')).join('');
}

function stableJson(value: unknown): string {
  if (value === null || typeof value !== 'object') {
    return JSON.stringify(value);
  }
  if (Array.isArray(value)) {
    return `[${value.map(stableJson).join(',')}]`;
  }
  const record = value as Record<string, unknown>;
  return `{${Object.keys(record).sort().map((key) => `${JSON.stringify(key)}:${stableJson(record[key])}`).join(',')}}`;
}

function utf8Bytes(value: string): number {
  return new TextEncoder().encode(value).byteLength;
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

function first<T>(cursor: { toArray(): T[] }): T | null {
  return cursor.toArray()[0] ?? null;
}

function unauthorized(): Response {
  return new Response('Unauthorized', { status: 401 });
}

function badRequest(): Response {
  return new Response('Invalid request', { status: 400 });
}
