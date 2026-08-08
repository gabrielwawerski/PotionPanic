import { SELF, env, reset, runInDurableObject } from 'cloudflare:test';
import { afterEach, describe, expect, it } from 'vitest';
import {
  MaximumEnvelopeBytes,
  type ClientEnvelope,
  type LeaseRecord,
  type PresenceRecord,
  type ServerEnvelope
} from '../../src/protocol';

const projectId = 'potion-panic';
const projectUrl = `https://example.test/v1/projects/${projectId}`;
const adminToken = 'test-admin-token';
const start = new Date(Date.now() + 5 * 60 * 1000);
const maximumSnapshotDataBytes = 256 * 1024;
const capacityRequestId = 'dddddddd-dddd-4ddd-8ddd-dddddddddddd';

interface SessionIdentity {
  sessionId: string;
  developerId: string;
  displayName: string;
  expiresAt: string;
}

interface Transition {
  requester: ServerEnvelope | ServerEnvelope[] | null;
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

    const outcomes = [requesterType(acquire), requesterType(reserve)].sort();
    expect(outcomes).toEqual(['lease.denied', 'lease.granted']);
    const currentState = await snapshot(rinConnection);
    expect(currentState.leases).toHaveLength(1);
    expect(currentState.leases[0]).toMatchObject({ path: 'assets/scenes/lab.unity' });
  });

  it('uses NFC, slash-normalized, ASCII-only path keys for lease ownership', async () => {
    const [rin, sol] = await Promise.all([createSessionIdentity('Rin'), createSessionIdentity('Sol')]);
    const [rinConnection, solConnection] = await openConnections([rin, sol]);

    const upperDiaeresis = await message(
      rinConnection,
      contextMessage('lease.acquire', 'Assets\\Scenes\\Ä.unity'),
      start
    );
    const lowerDiaeresis = await message(
      solConnection,
      contextMessage('lease.acquire', 'Assets/Scenes/ä.unity'),
      start
    );
    const decomposed = await message(
      rinConnection,
      contextMessage('lease.acquire', 'Assets/Scenes/Cafe\u0301.unity'),
      start
    );
    const composed = await message(
      solConnection,
      contextMessage('lease.acquire', 'Assets/Scenes/Café.unity'),
      start
    );

    expect(upperDiaeresis.requester).toMatchObject({ type: 'lease.granted' });
    expect(lowerDiaeresis.requester).toMatchObject({ type: 'lease.granted' });
    expect(decomposed.requester).toMatchObject({ type: 'lease.granted' });
    expect(composed.requester).toMatchObject({ type: 'lease.denied' });
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

  it('cancels a reservation from another connection owned by the same developer', async () => {
    const rin = await createSessionIdentity('Rin');
    const [firstConnection, secondConnection] = await openConnections([rin, rin]);
    const reserved = await message(
      firstConnection,
      contextMessage('lease.reserve', 'Assets/Scenes/Lab.unity'),
      start
    );
    const requestId = '33333333-3333-4333-8333-333333333333';

    const cancelled = await message(
      secondConnection,
      { ...pathMessage('reservation.cancel', 'Assets/Scenes/Lab.unity'), requestId },
      plusSeconds(start, 1)
    );
    const state = await snapshot(secondConnection, plusSeconds(start, 1));

    expect(cancelled).toMatchObject({
      stateVersion: reserved.stateVersion + 1,
      requester: {
        type: 'lease.released',
        requestId,
        path: 'assets/scenes/lab.unity'
      },
      stateChanges: [{ type: 'lease.released', requestId }]
    });
    expect(state.leases).toEqual([]);
  });

  it('denies reservation cancellation by another developer without changing state', async () => {
    const [rin, sol] = await Promise.all([
      createSessionIdentity('Rin'),
      createSessionIdentity('Sol')
    ]);
    const [rinConnection, solConnection] = await openConnections([rin, sol]);
    const reserved = await message(
      rinConnection,
      contextMessage('lease.reserve', 'Assets/Scenes/Lab.unity'),
      start
    );

    const denied = await message(
      solConnection,
      pathMessage('reservation.cancel', 'Assets/Scenes/Lab.unity'),
      plusSeconds(start, 1)
    );
    const state = await snapshot(solConnection, plusSeconds(start, 1));

    expect(denied).toMatchObject({
      stateVersion: reserved.stateVersion,
      requester: {
        type: 'lease.denied',
        code: 'reservation_not_owned',
        currentLease: { developerId: rin.developerId, mode: 'reserved' }
      },
      stateChanges: []
    });
    expect(state.leases).toEqual([
      expect.objectContaining({ developerId: rin.developerId, mode: 'reserved' })
    ]);
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

  it('bounds accepted state by the complete correlated snapshot chunk sequence', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    const { accepted, next } = await seedLargestAcceptedReservationSet(rin, connection);
    const request = {
      ...contextMessage('lease.reserve', next.path),
      branch: next.branch,
      task: next.task
    } as Extract<ClientEnvelope, { type: 'lease.reserve' }>;
    const acceptedChunks = await snapshotChunks(connection, capacityRequestId);
    const before = await persistedState();

    expect(snapshotSequenceBytes(acceptedChunks)).toBeLessThanOrEqual(maximumSnapshotDataBytes);
    expect(snapshotDataBytes([], [...accepted, next])).toBeLessThanOrEqual(
      maximumSnapshotDataBytes
    );
    await replaceReservations([...accepted, next]);
    const rejectedChunks = await snapshotChunks(connection, capacityRequestId);
    expect(snapshotSequenceBytes(rejectedChunks)).toBeGreaterThan(maximumSnapshotDataBytes);
    await replaceReservations(accepted);

    const rejected = await message(connection, request, start);
    const after = await persistedState();

    expect(rejected).toMatchObject({
      requester: {
        type: 'error',
        requestId: request.requestId,
        code: 'state_capacity_exceeded'
      },
      stateChanges: [],
      stateVersion: before.stateVersion
    });
    expect(after).toEqual(before);
  });

  it('keeps near-limit accepted state safe at the maximum valid state version', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    const { accepted } = await seedLargestAcceptedReservationSet(rin, connection);
    const base = accepted.slice(0, -1);
    const candidate = await largestCurrentVersionCandidate(
      rin,
      connection,
      base,
      accepted[accepted.length - 1]
    );
    const initialVersion = (await persistedState()).stateVersion;

    await replaceReservations([...base, candidate]);
    await setStateVersion(Number.MAX_SAFE_INTEGER);
    expect(snapshotSequenceBytes(
      await snapshotChunks(connection, capacityRequestId)
    )).toBeGreaterThan(maximumSnapshotDataBytes);

    await setStateVersion(initialVersion);
    await replaceReservations(base);
    const request = {
      ...contextMessage('lease.reserve', candidate.path),
      branch: candidate.branch,
      task: candidate.task
    } as Extract<ClientEnvelope, { type: 'lease.reserve' }>;
    const rejected = await message(connection, request, start);

    expect(rejected.requester).toMatchObject({
      type: 'error',
      code: 'state_capacity_exceeded'
    });
    await setStateVersion(Number.MAX_SAFE_INTEGER);
    expect(snapshotSequenceBytes(
      await snapshotChunks(connection, capacityRequestId)
    )).toBeLessThanOrEqual(maximumSnapshotDataBytes);
  });

  it('rolls back a mutation whose presence broadcast would exceed 16 KiB', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    const path = `Assets/${'A'.repeat(1017)}`;
    const request = {
      ...contextMessage('presence.open', path),
      branch: 'b'.repeat(256),
      task: 't'.repeat(256)
    } as Extract<ClientEnvelope, { type: 'presence.open' }>;
    const existing = maximumPresenceRecords(path, rin, connection, request);
    await insertPresence(existing.slice(0, -1));
    const before = await persistedState();

    expect(utf8Bytes(JSON.stringify({
      protocolVersion: 1,
      type: 'presence.updated',
      stateVersion: before.stateVersion + 1,
      requestId: request.requestId,
      presence: existing
    }))).toBeGreaterThan(MaximumEnvelopeBytes);

    const rejected = await message(connection, request, start);
    const after = await persistedState();

    expect(rejected).toMatchObject({
      requester: {
        type: 'error',
        requestId: request.requestId,
        code: 'state_capacity_exceeded'
      },
      stateChanges: [],
      stateVersion: before.stateVersion
    });
    expect(after).toEqual(before);
  });

  it('allows a shrinking mutation while existing snapshot data exceeds capacity', async () => {
    const rin = await createSessionIdentity('Rin');
    const [connection] = await openConnections([rin]);
    const path = 'assets/scenes/remove-at-capacity.unity';
    await seedPresenceAndReservationsOverCapacity(rin, connection, path);
    const before = await persistedState();

    const closed = await message(
      connection,
      pathMessage('presence.close', path),
      start
    );
    const after = await persistedState();

    expect(closed.requester).toMatchObject({ type: 'presence.removed', path });
    expect(after).toEqual({
      stateVersion: before.stateVersion + 1,
      presence: before.presence - 1,
      leases: before.leases
    });
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
  const envelopes = await snapshotChunks(connectionId, crypto.randomUUID(), now);
  return {
    ...envelopes[0],
    presence: envelopes.flatMap((envelope) => envelope.type === 'snapshot' ? envelope.presence : []),
    leases: envelopes.flatMap((envelope) => envelope.type === 'snapshot' ? envelope.leases : [])
  } as ServerEnvelope & {
    type: 'snapshot'; leases: LeaseRecord[]; presence: unknown[];
  };
}

async function snapshotChunks(
  connectionId: string,
  requestId: string,
  now = start
): Promise<Array<Extract<ServerEnvelope, { type: 'snapshot' }>>> {
  const transition = await message(connectionId, {
    protocolVersion: 1,
    type: 'snapshot.request',
    requestId
  }, now);
  if (!Array.isArray(transition.requester)) {
    throw new Error('Expected snapshot chunks.');
  }
  return transition.requester.map((envelope) => {
    if (envelope.type !== 'snapshot') {
      throw new Error('Expected only snapshot envelopes.');
    }
    return envelope;
  });
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

function pathMessage(
  type: 'presence.close' | 'lease.release' | 'reservation.cancel',
  path: string
): ClientEnvelope {
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

async function seedLargestAcceptedReservationSet(
  session: SessionIdentity,
  connectionId: string
): Promise<{ accepted: LeaseRecord[]; next: LeaseRecord }> {
  const fixtures = Array.from({ length: 600 }, (_, index) => reservationFixture(session, index));
  let low = 0;
  let high = fixtures.length;
  while (low < high) {
    const middle = Math.ceil((low + high) / 2);
    await replaceReservations(fixtures.slice(0, middle));
    const chunks = await snapshotChunks(connectionId, capacityRequestId);
    if (snapshotSequenceBytes(chunks) <= maximumSnapshotDataBytes) {
      low = middle;
    } else {
      high = middle - 1;
    }
  }
  if (low >= fixtures.length) {
    throw new Error('Reservation fixtures did not reach snapshot capacity.');
  }
  const accepted = fixtures.slice(0, low);
  await replaceReservations(accepted);
  return { accepted, next: fixtures[low] };
}

async function largestCurrentVersionCandidate(
  session: SessionIdentity,
  connectionId: string,
  base: LeaseRecord[],
  template: LeaseRecord
): Promise<LeaseRecord> {
  let low = template.path.length;
  let high = 1024;
  let candidate = template;
  while (low <= high) {
    const length = Math.floor((low + high) / 2);
    const path = `assets/${'x'.repeat(length - 'assets/'.length)}`;
    const next = { ...template, path, displayPath: path, developerId: session.developerId };
    await replaceReservations([...base, next]);
    const bytes = snapshotSequenceBytes(await snapshotChunks(connectionId, capacityRequestId));
    if (bytes <= maximumSnapshotDataBytes) {
      candidate = next;
      low = length + 1;
    } else {
      high = length - 1;
    }
  }
  return candidate;
}

async function setStateVersion(value: number): Promise<void> {
  await inspectProject((state) => {
    state.storage.sql.exec(
      "UPDATE coordination_state SET value = ? WHERE key = 'state_version'",
      value
    );
  });
}

function reservationFixture(session: SessionIdentity, index: number): LeaseRecord {
  const path = `assets/scenes/capacity-${index.toString().padStart(4, '0')}.unity`;
  return {
    leaseId: crypto.randomUUID(),
    path,
    displayPath: path,
    mode: 'reserved',
    developerId: session.developerId,
    displayName: session.displayName,
    branch: `feature/${'b'.repeat(240)}`,
    task: 't'.repeat(256),
    expiresAt: '2099-08-08T00:00:00.000Z'
  };
}

async function replaceReservations(leases: LeaseRecord[]): Promise<void> {
  await inspectProject((state) => {
    state.storage.sql.exec('DELETE FROM reservations');
    for (const lease of leases) {
      insertReservation(state, lease);
    }
  });
}

function maximumPresenceRecords(
  displayPath: string,
  session: SessionIdentity,
  connectionId: string,
  request: Extract<ClientEnvelope, { type: 'presence.open' }>
): PresenceRecord[] {
  const path = displayPath.toLowerCase();
  const candidate: PresenceRecord = {
    path,
    displayPath,
    developerId: session.developerId,
    displayName: session.displayName,
    connectionId,
    branch: request.branch,
    task: request.task,
    expiresAt: plusSeconds(start, 120).toISOString()
  };
  const records: PresenceRecord[] = [];
  while (utf8Bytes(JSON.stringify({
    protocolVersion: 1,
    type: 'presence.updated',
    stateVersion: 2,
    requestId: request.requestId,
    presence: [...records, candidate]
  })) <= MaximumEnvelopeBytes) {
    records.push({
      ...candidate,
      developerId: crypto.randomUUID(),
      displayName: 'Peer',
      connectionId: crypto.randomUUID()
    });
  }
  return [...records, candidate];
}

async function insertPresence(records: PresenceRecord[]): Promise<void> {
  await inspectProject((state) => {
    for (const presence of records) {
      state.storage.sql.exec(`
        INSERT INTO presence (
          canonical_path, display_path, developer_id, display_name, connection_id, branch, task,
          expires_at
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
      `,
      presence.path,
      presence.displayPath,
      presence.developerId,
      presence.displayName,
      presence.connectionId,
      presence.branch,
      presence.task,
      presence.expiresAt);
    }
  });
}

async function seedReservationsNearCapacity(
  session: SessionIdentity,
  targetBytes: number
): Promise<LeaseRecord[]> {
  const leases: LeaseRecord[] = [];
  while (true) {
    const index = leases.length;
    const path = `assets/scenes/capacity-${index.toString().padStart(4, '0')}.unity`;
    const lease: LeaseRecord = {
      leaseId: crypto.randomUUID(),
      path,
      displayPath: path,
      mode: 'reserved',
      developerId: session.developerId,
      displayName: session.displayName,
      branch: `feature/${'b'.repeat(240)}`,
      task: 't'.repeat(256),
      expiresAt: '2099-08-08T00:00:00.000Z'
    };
    if (snapshotDataBytes([], [...leases, lease]) > targetBytes) {
      break;
    }
    leases.push(lease);
  }
  await inspectProject((state) => {
    for (const lease of leases) {
      insertReservation(state, lease);
    }
  });
  return leases;
}

async function seedPresenceAndReservationsOverCapacity(
  session: SessionIdentity,
  connectionId: string,
  presencePath: string
): Promise<void> {
  const leases = await seedReservationsNearCapacity(session, maximumSnapshotDataBytes + 2 * 1024);
  const presence: PresenceRecord = {
    path: presencePath,
    displayPath: presencePath,
    developerId: session.developerId,
    displayName: session.displayName,
    connectionId,
    branch: 'feature/test',
    task: 'PP-7',
    expiresAt: plusSeconds(start, 120).toISOString()
  };
  expect(snapshotDataBytes([presence], leases)).toBeGreaterThan(maximumSnapshotDataBytes);
  await inspectProject((state) => {
    state.storage.sql.exec(`
      INSERT INTO presence (
        canonical_path, display_path, developer_id, display_name, connection_id, branch, task,
        expires_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    `,
    presence.path,
    presence.displayPath,
    presence.developerId,
    presence.displayName,
    presence.connectionId,
    presence.branch,
    presence.task,
    presence.expiresAt);
  });
}

function insertReservation(state: DurableObjectState, lease: LeaseRecord): void {
  state.storage.sql.exec(`
    INSERT INTO reservations (
      reservation_id, canonical_path, display_path, developer_id, display_name, branch, task,
      created_at, expires_at
    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
  `,
  lease.leaseId,
  lease.path,
  lease.displayPath,
  lease.developerId,
  lease.displayName,
  lease.branch,
  lease.task,
  '2026-08-08T00:00:00.000Z',
  lease.expiresAt);
}

async function persistedState(): Promise<{
  stateVersion: number;
  presence: number;
  leases: number;
}> {
  return inspectProject((state) => ({
    stateVersion: state.storage.sql.exec<{ value: number }>(
      "SELECT value FROM coordination_state WHERE key = 'state_version'"
    ).one().value,
    presence: state.storage.sql.exec<{ count: number }>(
      'SELECT COUNT(*) AS count FROM presence'
    ).one().count,
    leases: state.storage.sql.exec<{ count: number }>(
      'SELECT COUNT(*) AS count FROM leases'
    ).one().count + state.storage.sql.exec<{ count: number }>(
      'SELECT COUNT(*) AS count FROM reservations'
    ).one().count
  }));
}

function snapshotDataBytes(presence: PresenceRecord[], leases: LeaseRecord[]): number {
  return utf8Bytes(JSON.stringify({ presence, leases }));
}

function snapshotSequenceBytes(envelopes: ServerEnvelope[]): number {
  return envelopes.reduce((bytes, envelope) => bytes + utf8Bytes(JSON.stringify(envelope)), 0);
}

function requesterType(transition: Transition): ServerEnvelope['type'] | undefined {
  return Array.isArray(transition.requester)
    ? transition.requester[0]?.type
    : transition.requester?.type;
}

function utf8Bytes(value: string): number {
  return new TextEncoder().encode(value).byteLength;
}

function bearerHeaders(token: string): HeadersInit {
  return { authorization: `Bearer ${token}` };
}

function plusSeconds(date: Date, seconds: number): Date {
  return new Date(date.getTime() + seconds * 1000);
}
