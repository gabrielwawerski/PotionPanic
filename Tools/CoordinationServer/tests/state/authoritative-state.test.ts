import { SELF, env, reset, runInDurableObject } from 'cloudflare:test';
import { afterEach, describe, expect, it } from 'vitest';
import type { ClientEnvelope, LeaseRecord, ServerEnvelope } from '../../src/protocol';

const projectId = 'potion-panic';
const projectUrl = `https://example.test/v1/projects/${projectId}`;
const adminToken = 'test-admin-token';
const start = new Date(Date.now() + 5 * 60 * 1000);

interface SessionIdentity {
  sessionId: string;
  developerId: string;
  displayName: string;
  expiresAt: string;
}

interface Transition {
  requester: ServerEnvelope | null;
  stateChanges: ServerEnvelope[];
  stateVersion: number;
}

interface StateMachine {
  openConnection(session: SessionIdentity, now: Date): Promise<Transition>;
  closeConnection(connectionId: string, now: Date): Promise<Transition>;
  handleMessage(
    connectionId: string,
    message: ClientEnvelope,
    now: Date
  ): Promise<Transition>;
  pruneExpired(now: Date): Promise<Transition>;
}

afterEach(async () => {
  await reset();
});

describe('authoritative coordination state', () => {
  it('creates a connection and returns session readiness with a monotonic state version', async () => {
    const rin = await createSessionIdentity('Rin');

    const opened = await withMachine((machine) => machine.openConnection(rin, start));

    expect(opened).toMatchObject({
      stateVersion: 1,
      requester: {
        protocolVersion: 1,
        type: 'session.ready',
        stateVersion: 1,
        developerId: rin.developerId,
        displayName: 'Rin',
        serverTime: start.toISOString(),
        leaseTtlSeconds: 120,
        reservationTtlSeconds: 1800
      },
      stateChanges: []
    });
    expect((opened.requester as ServerEnvelope & { connectionId: string }).connectionId)
      .toMatch(/^[0-9a-f-]{36}$/i);
  });

  it('serializes competing acquire and reserve requests into one effective owner', async () => {
    const [rin, sol] = await Promise.all([createSessionIdentity('Rin'), createSessionIdentity('Sol')]);
    const [rinConnection, solConnection] = await openConnections([rin, sol]);

    const [acquire, reserve] = await withMachine((machine) => Promise.all([
      machine.handleMessage(rinConnection, contextMessage('lease.acquire', 'Assets/Scenes/Lab.unity'), start),
      machine.handleMessage(solConnection, contextMessage('lease.reserve', 'Assets/Scenes/Lab.unity'), start)
    ]));

    const outcomes = [acquire.requester?.type, reserve.requester?.type].sort();
    expect(outcomes).toEqual(['lease.denied', 'lease.granted']);
    const currentState = await snapshot(rinConnection);
    expect(currentState.leases).toHaveLength(1);
    expect(currentState.leases[0]).toMatchObject({ path: 'assets/scenes/lab.unity' });
  });

  it('extends only the heartbeating connection presence and editing leases', async () => {
    const rin = await createSessionIdentity('Rin');
    const [firstConnection, secondConnection] = await openConnections([rin, rin]);
    await message(firstConnection, contextMessage('presence.open', 'Assets/Scenes/First.unity'), start);
    await message(firstConnection, contextMessage('lease.acquire', 'Assets/Scenes/First.unity'), start);
    await message(secondConnection, contextMessage('presence.open', 'Assets/Scenes/Second.unity'), start);
    await message(secondConnection, contextMessage('lease.acquire', 'Assets/Scenes/Second.unity'), start);

    await message(firstConnection, baseMessage('heartbeat'), plusSeconds(start, 60));
    const state = await snapshot(firstConnection, plusSeconds(start, 60));
    const firstLease = state.leases.find((lease) => lease.path.endsWith('first.unity')) as LeaseRecord;
    const secondLease = state.leases.find((lease) => lease.path.endsWith('second.unity')) as LeaseRecord;

    expect(firstLease.expiresAt).toBe(plusSeconds(start, 180).toISOString());
    expect(secondLease.expiresAt).toBe(plusSeconds(start, 120).toISOString());
  });

  it('prunes expired sessions, connections, presence, editing leases, reservations, and replay records', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    await message(connection, contextMessage('presence.open', 'Assets/Scenes/Lab.unity'), start);
    await message(connection, contextMessage('lease.acquire', 'Assets/Scenes/Lab.unity'), start);
    await message(connection, contextMessage('lease.reserve', 'Assets/Scenes/Reserved.unity'), start);
    await message(connection, baseMessage('heartbeat'), start);

    const expired = await withMachine((machine) => machine.pruneExpired(plusSeconds(start, 24 * 60 * 60 + 1)));
    const persisted = await inspectProject((state) => [
      'sessions', 'connections', 'presence', 'leases', 'reservations', 'replay_records'
    ].reduce((count, table) => count + state.storage.sql.exec<{ count: number }>(
      `SELECT COUNT(*) AS count FROM ${table}`
    ).one().count, 0));

    expect(expired.stateChanges.map(({ type }) => type)).toEqual(expect.arrayContaining([
      'presence.removed', 'lease.released'
    ]));
    expect(persisted).toBe(0);
  });

  it('rejects a request identifier reused with a different payload without changing state', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    const requestId = '11111111-1111-4111-8111-111111111111';
    await message(connection, { ...contextMessage('presence.open', 'Assets/Scenes/Lab.unity'), requestId }, start);

    const mismatch = await message(
      connection,
      { ...pathMessage('presence.close', 'Assets/Scenes/Lab.unity'), requestId },
      start
    );
    const state = await snapshot(connection, start);

    expect(mismatch).toMatchObject({
      requester: { type: 'error', code: 'replay_payload_mismatch' }
    });
    expect(state.presence).toHaveLength(1);
  });

  it('rolls back a failed replay insertion without persisting a lease or state version', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    const before = await inspectProject((state) => state.storage.sql.exec<{ value: number }>(
      "SELECT value FROM coordination_state WHERE key = 'state_version'"
    ).one().value);
    await inspectProject((state) => {
      state.storage.sql.exec(`
        CREATE TRIGGER abort_replay_insert
        BEFORE INSERT ON replay_records
        BEGIN
          SELECT RAISE(ABORT, 'replay insert failure');
        END
      `);
    });

    await expect(message(
      connection,
      contextMessage('lease.acquire', 'Assets/Scenes/Atomic.unity'),
      start
    )).rejects.toThrow('replay insert failure');

    const after = await inspectProject((state) => ({
      stateVersion: state.storage.sql.exec<{ value: number }>(
        "SELECT value FROM coordination_state WHERE key = 'state_version'"
      ).one().value,
      leases: state.storage.sql.exec<{ count: number }>(
        "SELECT COUNT(*) AS count FROM leases WHERE canonical_path = 'assets/scenes/atomic.unity'"
      ).one().count
    }));

    expect(after).toEqual({ stateVersion: before, leases: 0 });
  });

  it('converts its owner reservation to an editing lease and restores the reservation on close', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    await message(connection, contextMessage('lease.reserve', 'Assets/Scenes/Lab.unity'), start);
    const acquired = await message(connection, contextMessage('lease.acquire', 'Assets/Scenes/Lab.unity'), start);
    await withMachine((machine) => machine.closeConnection(connection, plusSeconds(start, 1)));
    const state = await snapshotVia(rin, plusSeconds(start, 1));

    expect(acquired.requester).toMatchObject({ type: 'lease.granted', lease: { mode: 'editing' } });
    expect(state.leases).toEqual([expect.objectContaining({
      mode: 'reserved',
      developerId: rin.developerId
    })]);
    expect(state.leases[0]).not.toHaveProperty('connectionId');
  });

  it('rebinds an editing lease to a same-developer reconnect and rejects stale release attempts', async () => {
    const rin = await createSessionIdentity('Rin');
    const [firstConnection, secondConnection] = await openConnections([rin, rin]);
    await message(firstConnection, contextMessage('lease.acquire', 'Assets/Scenes/Lab.unity'), start);
    const rebound = await message(
      secondConnection,
      contextMessage('lease.acquire', 'Assets/Scenes/Lab.unity'),
      plusSeconds(start, 1)
    );
    const staleRelease = await message(
      firstConnection,
      pathMessage('lease.release', 'Assets/Scenes/Lab.unity'),
      plusSeconds(start, 2)
    );
    const state = await snapshot(secondConnection, plusSeconds(start, 2));

    expect(rebound.requester).toMatchObject({ type: 'lease.granted', lease: { connectionId: secondConnection } });
    expect(staleRelease.requester).toMatchObject({ type: 'lease.denied', code: 'lease_not_owned' });
    expect(state.leases).toEqual([expect.objectContaining({ connectionId: secondConnection })]);
  });

  it('overrides a remote lease and prevents the displaced connection from releasing it', async () => {
    const [rin, sol] = await Promise.all([createSessionIdentity('Rin'), createSessionIdentity('Sol')]);
    const [rinConnection, solConnection] = await openConnections([rin, sol]);
    await message(rinConnection, contextMessage('lease.acquire', 'Assets/Scenes/Lab.unity'), start);
    const override = await message(
      solConnection,
      contextMessage('lease.override', 'Assets/Scenes/Lab.unity'),
      plusSeconds(start, 1)
    );
    await withMachine((machine) => machine.closeConnection(rinConnection, plusSeconds(start, 2)));
    const state = await snapshot(solConnection, plusSeconds(start, 2));

    expect(override.requester).toMatchObject({
      type: 'lease.overridden',
      previousDeveloperId: rin.developerId,
      lease: { developerId: sol.developerId, connectionId: solConnection }
    });
    expect(state.leases).toEqual([expect.objectContaining({ developerId: sol.developerId })]);
  });

  it('retains the current version for snapshots and schedules the nearest state expiry alarm', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    await message(connection, contextMessage('presence.open', 'Assets/Scenes/Lab.unity'), start);
    const first = await snapshot(connection, start);
    const second = await snapshot(connection, start);
    const alarm = await inspectProject((state) => state.storage.getAlarm());

    expect(second.stateVersion).toBe(first.stateVersion);
    expect(alarm).toBe(plusSeconds(start, 120).getTime());
  });
});

async function createSessionIdentity(displayName: string): Promise<SessionIdentity> {
  const developer = await createDeveloper(displayName);
  const response = await SELF.fetch(`${projectUrl}/sessions`, {
    method: 'POST',
    headers: bearerHeaders(developer.developerToken)
  });
  expect(response.status).toBe(201);
  const session = await inspectProject((state) => {
    const value = state.storage.sql.exec<{
      session_id: string;
      expires_at: string;
    }>('SELECT session_id, expires_at FROM sessions WHERE developer_id = ?', developer.developerId).one();
    const expiresAt = plusSeconds(start, 120).toISOString();
    state.storage.sql.exec(
      'UPDATE sessions SET expires_at = ? WHERE session_id = ?',
      expiresAt,
      value.session_id
    );
    return { ...value, expires_at: expiresAt };
  });

  return {
    sessionId: session.session_id,
    developerId: developer.developerId,
    displayName,
    expiresAt: session.expires_at
  };
}

async function createDeveloper(displayName: string): Promise<{
  developerId: string;
  developerToken: string;
}> {
  const response = await SELF.fetch(`${projectUrl}/developers`, {
    method: 'POST',
    headers: { ...bearerHeaders(adminToken), 'content-type': 'application/json' },
    body: JSON.stringify({ displayName })
  });
  expect(response.status).toBe(201);
  return response.json();
}

async function openConnections(sessions: SessionIdentity[]): Promise<string[]> {
  return Promise.all(sessions.map(async (session) => {
    const transition = await withMachine((machine) => machine.openConnection(session, start));
    return (transition.requester as ServerEnvelope & { connectionId: string }).connectionId;
  }));
}

async function message(connectionId: string, envelope: ClientEnvelope, now: Date): Promise<Transition> {
  return withMachine((machine) => machine.handleMessage(connectionId, envelope, now));
}

async function snapshot(connectionId: string, now = start): Promise<ServerEnvelope & {
  type: 'snapshot'; leases: LeaseRecord[]; presence: unknown[];
}> {
  const transition = await message(connectionId, baseMessage('snapshot.request'), now);
  return transition.requester as ServerEnvelope & {
    type: 'snapshot'; leases: LeaseRecord[]; presence: unknown[];
  };
}

async function snapshotVia(session: SessionIdentity, now: Date): Promise<ServerEnvelope & {
  type: 'snapshot'; leases: LeaseRecord[];
}> {
  const transition = await withMachine((machine) => machine.openConnection(session, now));
  const connectionId = (transition.requester as ServerEnvelope & { connectionId: string }).connectionId;
  return snapshot(connectionId, now);
}

function baseMessage(type: 'heartbeat' | 'snapshot.request'): ClientEnvelope {
  return { protocolVersion: 1, type, requestId: crypto.randomUUID() };
}

function contextMessage(
  type: 'presence.open' | 'lease.acquire' | 'lease.reserve' | 'lease.override',
  path: string
): ClientEnvelope {
  return {
    protocolVersion: 1,
    type,
    requestId: crypto.randomUUID(),
    path,
    branch: 'feature/test',
    task: 'PP-7'
  };
}

function pathMessage(type: 'presence.close' | 'lease.release', path: string): ClientEnvelope {
  return { protocolVersion: 1, type, requestId: crypto.randomUUID(), path };
}

async function withMachine<T>(callback: (machine: StateMachine) => T | Promise<T>): Promise<T> {
  const objectNamespace = (env as unknown as {
    COORDINATION_OBJECT: DurableObjectNamespace;
  }).COORDINATION_OBJECT;
  const object = objectNamespace.get(objectNamespace.idFromName(projectId));
  return runInDurableObject(object, (instance) => callback(instance as unknown as StateMachine));
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

function plusSeconds(date: Date, seconds: number): Date {
  return new Date(date.getTime() + seconds * 1000);
}
