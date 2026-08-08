import {
  SELF,
  env,
  evictDurableObject,
  reset,
  runDurableObjectAlarm,
  runInDurableObject
} from 'cloudflare:test';
import { afterEach, describe, expect, it } from 'vitest';
import {
  MaximumEnvelopeBytes,
  VersionedServerState,
  type ServerEnvelope
} from '../../src/protocol';

const projectId = 'potion-panic';
const projectUrl = `https://example.test/v1/projects/${projectId}`;
const adminToken = 'test-admin-token';

type SnapshotChunk = Extract<ServerEnvelope, { type: 'snapshot' }> & {
  snapshotId: string;
  chunkIndex: number;
  chunkCount: number;
};

afterEach(async () => {
  await reset();
});

describe('coordination WebSocket synchronization', () => {
  it('accepts an authenticated upgrade', async () => {
    const developer = await createDeveloper('Rin');
    const sessionToken = await createSession(developer.developerToken);

    const response = await SELF.fetch(`${projectUrl}/connect`, {
      headers: {
        authorization: `Bearer ${sessionToken}`,
        upgrade: 'websocket'
      }
    });

    expect(response.status).toBe(101);
  });

  it('rejects an upgrade when a token appears in the query string', async () => {
    const developer = await createDeveloper('Rin');
    const sessionToken = await createSession(developer.developerToken);

    const response = await SELF.fetch(`${projectUrl}/connect?token=${sessionToken}`, {
      headers: {
        authorization: `Bearer ${sessionToken}`,
        upgrade: 'websocket'
      }
    });

    expect(response.status).toBe(400);
  });

  it('sends readiness before the current snapshot after an upgrade', async () => {
    const developer = await createDeveloper('Rin');
    const sessionToken = await createSession(developer.developerToken);
    const response = await SELF.fetch(`${projectUrl}/connect`, {
      headers: {
        authorization: `Bearer ${sessionToken}`,
        upgrade: 'websocket'
      }
    });
    const socket = response.webSocket;
    if (socket === null) {
      throw new Error('Expected an upgraded WebSocket.');
    }
    socket.accept();

    const messages = await collectMessages(socket, 2);

    expect(messages.map(({ type }) => type)).toEqual(['session.ready', 'snapshot']);
    expect(messages[0]).toMatchObject({
      developerId: developer.developerId,
      displayName: 'Rin',
      leaseTtlSeconds: 120,
      reservationTtlSeconds: 1800
    });
    expect(messages[1]).toMatchObject({
      snapshotId: expect.stringMatching(/^[0-9a-f-]{36}$/i),
      chunkIndex: 0,
      chunkCount: 1,
      presence: [],
      leases: []
    });
    expect(messages[1].stateVersion).toBe(messages[0].stateVersion);
  });

  it('chunks every snapshot path and replays a heartbeat chunk sequence exactly', async () => {
    const rin = await createDeveloper('Rin');
    const sol = await createDeveloper('Sol');
    await seedReservations(rin, 48);
    const socket = await connect(rin.developerToken);
    const solSocket = await connect(sol.developerToken);
    const initial = collectSnapshotExchange(socket, true);
    const solInitial = collectSnapshotExchange(solSocket, true);
    socket.accept();
    solSocket.accept();

    const initialMessages = await initial;
    await solInitial;
    expect(initialMessages[0]?.type).toBe('session.ready');
    assertSnapshotChunks(initialMessages.slice(1), undefined, 48);

    const snapshotRequestId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
    const requested = collectSnapshotExchange(socket);
    socket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'snapshot.request',
      requestId: snapshotRequestId
    }));
    assertSnapshotChunks(await requested, snapshotRequestId, 48);

    const presenceRequest = JSON.stringify({
      protocolVersion: 1,
      type: 'presence.open',
      requestId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
      path: 'Assets/Scenes/Active.unity',
      branch: 'feature/test',
      task: 'PP-7'
    });
    const rinPresence = collectMessages(socket, 1);
    const solPresence = collectMessages(solSocket, 1);
    socket.send(presenceRequest);
    await Promise.all([rinPresence, solPresence]);

    const leaseRequest = JSON.stringify({
      protocolVersion: 1,
      type: 'lease.acquire',
      requestId: 'ffffffff-ffff-4fff-8fff-ffffffffffff',
      path: 'Assets/Scenes/Active.unity',
      branch: 'feature/test',
      task: 'PP-7'
    });
    const rinLease = collectMessages(socket, 1);
    const solLease = collectMessages(solSocket, 1);
    socket.send(leaseRequest);
    await Promise.all([rinLease, solLease]);

    const heartbeatRequest = JSON.stringify({
      protocolVersion: 1,
      type: 'heartbeat',
      requestId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'
    });
    const firstHeartbeat = collectHeartbeatExchange(socket, 2);
    const solHeartbeat = collectMessages(solSocket, 2);
    socket.send(heartbeatRequest);
    const [firstHeartbeatMessages, solBroadcasts] = await Promise.all([
      firstHeartbeat,
      solHeartbeat
    ]);
    const chunkCount = (firstHeartbeatMessages[0] as SnapshotChunk).chunkCount;
    const firstHeartbeatChunks = firstHeartbeatMessages.slice(0, chunkCount);
    const firstBroadcasts = firstHeartbeatMessages.slice(chunkCount);
    assertSnapshotChunks(firstHeartbeatChunks, 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', 49, 1);
    expect(firstBroadcasts.map(({ type }) => type)).toEqual([
      'presence.updated',
      'lease.updated'
    ]);
    expect(solBroadcasts).toEqual(firstBroadcasts);

    const replayedHeartbeat = collectSnapshotExchange(socket);
    const solReplay = collectMessages(solSocket, 1);
    socket.send(heartbeatRequest);
    expect(await replayedHeartbeat).toEqual(firstHeartbeatChunks);
    expect(await solReplay).toEqual([]);
  });

  it('routes a client mutation without a client state version and broadcasts the transition', async () => {
    const rin = await createDeveloper('Rin');
    const sol = await createDeveloper('Sol');
    const rinSocket = await connect(rin.developerToken);
    const solSocket = await connect(sol.developerToken);
    const rinInitial = collectMessages(rinSocket, 2);
    const solInitial = collectMessages(solSocket, 2);
    rinSocket.accept();
    solSocket.accept();
    await Promise.all([rinInitial, solInitial]);

    const rinUpdate = collectMessages(rinSocket, 1);
    const solUpdate = collectMessages(solSocket, 1);
    rinSocket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'presence.open',
      requestId: '11111111-1111-4111-8111-111111111111',
      path: 'Assets/Scenes/Lab.unity',
      branch: 'feature/test',
      task: 'PP-7'
    }));

    const [[rinMessage], [solMessage]] = await Promise.all([rinUpdate, solUpdate]);

    expect(rinMessage).toEqual(solMessage);
    expect(rinMessage).toMatchObject({
      type: 'presence.updated',
      requestId: '11111111-1111-4111-8111-111111111111',
      stateVersion: 3,
      presence: [expect.objectContaining({
        developerId: rin.developerId,
        path: 'assets/scenes/lab.unity'
      })]
    });
  });

  it('restores server-derived socket metadata after hibernation', async () => {
    const developer = await createDeveloper('Rin');
    const socket = await connect(developer.developerToken);
    const initial = collectMessages(socket, 2);
    socket.accept();
    await initial;

    await evictDurableObject(projectObject());
    const snapshot = collectMessages(socket, 1);
    socket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'snapshot.request',
      requestId: '33333333-3333-4333-8333-333333333333'
    }));

    expect(await snapshot).toEqual([expect.objectContaining({
      type: 'snapshot',
      requestId: '33333333-3333-4333-8333-333333333333'
    })]);
  });

  it('returns a duplicate request result without rebroadcasting the prior state change', async () => {
    const rin = await createDeveloper('Rin');
    const sol = await createDeveloper('Sol');
    const rinSocket = await connect(rin.developerToken);
    const solSocket = await connect(sol.developerToken);
    const rinInitial = collectMessages(rinSocket, 2);
    const solInitial = collectMessages(solSocket, 2);
    rinSocket.accept();
    solSocket.accept();
    await Promise.all([rinInitial, solInitial]);

    const request = JSON.stringify({
      protocolVersion: 1,
      type: 'presence.open',
      requestId: '44444444-4444-4444-8444-444444444444',
      path: 'Assets/Scenes/Lab.unity',
      branch: 'feature/test',
      task: 'PP-7'
    });
    const firstRin = collectMessages(rinSocket, 1);
    const firstSol = collectMessages(solSocket, 1);
    rinSocket.send(request);
    const [[firstResult], [firstBroadcast]] = await Promise.all([firstRin, firstSol]);

    const duplicateRin = collectMessages(rinSocket, 1);
    const duplicateSol = collectMessages(solSocket, 1);
    rinSocket.send(request);

    expect(await duplicateRin).toEqual([firstResult]);
    expect(await duplicateSol).toEqual([]);
    expect(firstBroadcast).toEqual(firstResult);
  });

  it('broadcasts reservation cancellation and replays it only to the requester', async () => {
    const rin = await createDeveloper('Rin');
    const sol = await createDeveloper('Sol');
    const rinSocket = await connect(rin.developerToken);
    const solSocket = await connect(sol.developerToken);
    const rinInitial = collectMessages(rinSocket, 2);
    const solInitial = collectMessages(solSocket, 2);
    rinSocket.accept();
    solSocket.accept();
    await Promise.all([rinInitial, solInitial]);

    const reservedByRin = collectMessages(rinSocket, 1);
    const reservedBySol = collectMessages(solSocket, 1);
    rinSocket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'lease.reserve',
      requestId: '99999999-9999-4999-8999-999999999999',
      path: 'Assets/Scenes/Lab.unity',
      branch: 'feature/test',
      task: 'PP-7'
    }));
    await Promise.all([reservedByRin, reservedBySol]);

    const request = JSON.stringify({
      protocolVersion: 1,
      type: 'reservation.cancel',
      requestId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
      path: 'Assets/Scenes/Lab.unity'
    });
    const firstRin = collectMessages(rinSocket, 1);
    const firstSol = collectMessages(solSocket, 1);
    rinSocket.send(request);
    const [[firstResult], [firstBroadcast]] = await Promise.all([firstRin, firstSol]);

    const replayRin = collectMessages(rinSocket, 1);
    const replaySol = collectMessages(solSocket, 1);
    rinSocket.send(request);

    expect(firstResult).toMatchObject({
      type: 'lease.released',
      requestId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
      path: 'assets/scenes/lab.unity'
    });
    expect(firstBroadcast).toEqual(firstResult);
    expect(await replayRin).toEqual([firstResult]);
    expect(await replaySol).toEqual([]);
  });

  it('returns a replayed request response after a newer state exists without rebroadcasting it', async () => {
    const rin = await createDeveloper('Rin');
    const sol = await createDeveloper('Sol');
    const rinSocket = await connect(rin.developerToken);
    const solSocket = await connect(sol.developerToken);
    const rinInitial = collectMessages(rinSocket, 2);
    const solInitial = collectMessages(solSocket, 2);
    rinSocket.accept();
    solSocket.accept();
    await Promise.all([rinInitial, solInitial]);

    const request = JSON.stringify({
      protocolVersion: 1,
      type: 'presence.open',
      requestId: '77777777-7777-4777-8777-777777777777',
      path: 'Assets/Scenes/Lab.unity',
      branch: 'feature/test',
      task: 'PP-7'
    });
    const firstRin = collectMessages(rinSocket, 1);
    const firstSol = collectMessages(solSocket, 1);
    rinSocket.send(request);
    const [[firstResponse]] = await Promise.all([firstRin, firstSol]);

    const newerRin = collectMessages(rinSocket, 1);
    const newerSol = collectMessages(solSocket, 1);
    solSocket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'presence.open',
      requestId: '88888888-8888-4888-8888-888888888888',
      path: 'Assets/Scenes/Other.unity',
      branch: 'feature/test',
      task: 'PP-7'
    }));
    await Promise.all([newerRin, newerSol]);

    const replayRin = collectMessages(rinSocket, 1);
    const replaySol = collectMessages(solSocket, 1);
    rinSocket.send(request);

    expect(await replayRin).toEqual([firstResponse]);
    expect(await replaySol).toEqual([]);
  });

  it('rejects client-supplied state versions before applying a mutation', async () => {
    const developer = await createDeveloper('Rin');
    const socket = await connect(developer.developerToken);
    const initial = collectMessages(socket, 2);
    socket.accept();
    await initial;

    const error = collectMessages(socket, 1);
    socket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'presence.open',
      requestId: '55555555-5555-4555-8555-555555555555',
      path: 'Assets/Scenes/Lab.unity',
      branch: 'feature/test',
      task: 'PP-7',
      stateVersion: 0
    }));

    expect(await error).toEqual([expect.objectContaining({
      type: 'error',
      code: 'invalid_envelope'
    })]);
  });

  it('rejects oversized WebSocket envelopes without applying them', async () => {
    const developer = await createDeveloper('Rin');
    const socket = await connect(developer.developerToken);
    const initial = collectMessages(socket, 2);
    socket.accept();
    await initial;

    const error = collectMessages(socket, 1);
    socket.send('x'.repeat(16 * 1024 + 1));

    expect(await error).toEqual([expect.objectContaining({
      type: 'error',
      code: 'envelope_too_large'
    })]);
  });

  it('broadcasts authoritative expiry transitions from the durable object alarm', async () => {
    const rin = await createDeveloper('Rin');
    const sol = await createDeveloper('Sol');
    const rinSocket = await connect(rin.developerToken);
    const solSocket = await connect(sol.developerToken);
    const rinInitial = collectMessages(rinSocket, 2);
    const solInitial = collectMessages(solSocket, 2);
    rinSocket.accept();
    solSocket.accept();
    const [rinInitialMessages] = await Promise.all([rinInitial, solInitial]);
    const rinReady = rinInitialMessages[0] as Extract<ServerEnvelope, { type: 'session.ready' }>;

    const rinPresence = collectMessages(rinSocket, 1);
    const solPresence = collectMessages(solSocket, 1);
    rinSocket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'presence.open',
      requestId: '66666666-6666-4666-8666-666666666666',
      path: 'Assets/Scenes/Lab.unity',
      branch: 'feature/test',
      task: 'PP-7'
    }));
    await Promise.all([rinPresence, solPresence]);

    await runInDurableObject(projectObject(), (_instance, state) => {
      state.storage.sql.exec(
        "UPDATE connections SET expires_at = '2000-01-01T00:00:00.000Z' WHERE connection_id = ?",
        rinReady.connectionId
      );
      state.storage.sql.exec(
        "UPDATE presence SET expires_at = '2000-01-01T00:00:00.000Z' WHERE connection_id = ?",
        rinReady.connectionId
      );
    });
    const rinClosed = collectClose(rinSocket);
    const rinExpiry = collectMessages(rinSocket, 1);
    const solExpiry = collectMessages(solSocket, 1);

    expect(await runDurableObjectAlarm(projectObject())).toBe(true);
    expect(await rinClosed).toMatchObject({ code: 4001 });
    expect(await rinExpiry).toEqual([]);
    expect(await solExpiry).toEqual([expect.objectContaining({
      type: 'presence.removed',
      connectionId: rinReady.connectionId
    })]);
    expect(solSocket.readyState).toBe(WebSocket.OPEN);
  });

  it('rejects older server state when a client has already applied a newer version', () => {
    const state = new VersionedServerState();
    const newest: ServerEnvelope = {
      protocolVersion: 1,
      type: 'snapshot',
      stateVersion: 2,
      snapshotId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
      chunkIndex: 0,
      chunkCount: 1,
      presence: [],
      leases: [],
      serverTime: '2026-08-06T00:00:00.000Z'
    };
    const older: ServerEnvelope = { ...newest, stateVersion: 1 };

    expect(state.tryApply({ ok: true, value: newest })).toBe(true);
    expect(state.tryApply({ ok: true, value: older })).toBe(false);
  });

  it('revokes only the developer sockets and broadcasts their released presence', async () => {
    const rin = await createDeveloper('Rin');
    const sol = await createDeveloper('Sol');
    const rinSocket = await connect(rin.developerToken);
    const solSocket = await connect(sol.developerToken);
    const rinInitial = collectMessages(rinSocket, 2);
    const solInitial = collectMessages(solSocket, 2);
    rinSocket.accept();
    solSocket.accept();
    const [rinInitialMessages, solInitialMessages] = await Promise.all([rinInitial, solInitial]);
    const rinReady = rinInitialMessages[0] as Extract<ServerEnvelope, { type: 'session.ready' }>;
    const solReady = solInitialMessages[0] as Extract<ServerEnvelope, { type: 'session.ready' }>;

    const rinPresence = collectMessages(rinSocket, 1);
    const solPresence = collectMessages(solSocket, 1);
    rinSocket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'presence.open',
      requestId: '22222222-2222-4222-8222-222222222222',
      path: 'Assets/Scenes/Lab.unity',
      branch: 'feature/test',
      task: 'PP-7'
    }));
    await Promise.all([rinPresence, solPresence]);

    const rinClosed = collectClose(rinSocket);
    const solRemoval = collectMessages(solSocket, 1);
    const response = await SELF.fetch(`${projectUrl}/developers/${rin.developerId}`, {
      method: 'DELETE',
      headers: { authorization: `Bearer ${adminToken}` }
    });

    expect(response.status).toBe(204);
    expect(await rinClosed).toMatchObject({ code: 4003 });
    expect(await solRemoval).toEqual([expect.objectContaining({
      type: 'presence.removed',
      connectionId: rinReady.connectionId
    })]);
    expect(solSocket.readyState).toBe(WebSocket.OPEN);
    expect(solReady.connectionId).not.toBe(rinReady.connectionId);
  });

  it('revokes a developer reservation, broadcasts its release, and advances shared state', async () => {
    const rin = await createDeveloper('Rin');
    const sol = await createDeveloper('Sol');
    const rinSocket = await connect(rin.developerToken);
    const solSocket = await connect(sol.developerToken);
    const rinInitial = collectMessages(rinSocket, 2);
    const solInitial = collectMessages(solSocket, 2);
    rinSocket.accept();
    solSocket.accept();
    await Promise.all([rinInitial, solInitial]);

    const rinReserved = collectMessages(rinSocket, 1);
    const solReserved = collectMessages(solSocket, 1);
    rinSocket.send(JSON.stringify({
      protocolVersion: 1,
      type: 'lease.reserve',
      requestId: '99999999-9999-4999-8999-999999999999',
      path: 'Assets/Scenes/Reserved.unity',
      branch: 'feature/test',
      task: 'PP-7'
    }));
    await Promise.all([rinReserved, solReserved]);
    const before = await runInDurableObject(projectObject(), (_instance, state) => {
      return state.storage.sql.exec<{ value: number }>(
        "SELECT value FROM coordination_state WHERE key = 'state_version'"
      ).one().value;
    });

    const rinClosed = collectClose(rinSocket);
    const solRelease = collectMessages(solSocket, 1);
    const response = await SELF.fetch(`${projectUrl}/developers/${rin.developerId}`, {
      method: 'DELETE',
      headers: { authorization: `Bearer ${adminToken}` }
    });
    const after = await runInDurableObject(projectObject(), (_instance, state) => ({
      reservations: state.storage.sql.exec<{ count: number }>(
        'SELECT COUNT(*) AS count FROM reservations WHERE developer_id = ?',
        rin.developerId
      ).one().count,
      stateVersion: state.storage.sql.exec<{ value: number }>(
        "SELECT value FROM coordination_state WHERE key = 'state_version'"
      ).one().value
    }));

    expect(response.status).toBe(204);
    expect(await rinClosed).toMatchObject({ code: 4003 });
    expect(await solRelease).toEqual([expect.objectContaining({
      type: 'lease.released',
      path: 'assets/scenes/reserved.unity'
    })]);
    expect(after).toEqual({ reservations: 0, stateVersion: before + 1 });
  });
});

async function createDeveloper(displayName: string): Promise<{
  developerId: string;
  developerToken: string;
}> {
  const response = await SELF.fetch(`${projectUrl}/developers`, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${adminToken}`,
      'content-type': 'application/json'
    },
    body: JSON.stringify({ displayName })
  });

  expect(response.status).toBe(201);
  return response.json();
}

async function createSession(developerToken: string): Promise<string> {
  const response = await SELF.fetch(`${projectUrl}/sessions`, {
    method: 'POST',
    headers: { authorization: `Bearer ${developerToken}` }
  });

  expect(response.status).toBe(201);
  const body = await response.json() as { sessionToken: string };
  return body.sessionToken;
}

async function connect(developerToken: string): Promise<WebSocket> {
  const sessionToken = await createSession(developerToken);
  const response = await SELF.fetch(`${projectUrl}/connect`, {
    headers: {
      authorization: `Bearer ${sessionToken}`,
      upgrade: 'websocket'
    }
  });
  const socket = response.webSocket;
  if (socket === null) {
    throw new Error('Expected an upgraded WebSocket.');
  }
  return socket;
}

function projectObject(): DurableObjectStub {
  const objects = (env as unknown as { COORDINATION_OBJECT: DurableObjectNamespace })
    .COORDINATION_OBJECT;
  return objects.get(objects.idFromName(projectId));
}

function collectMessages(socket: WebSocket, expectedCount: number): Promise<ServerEnvelope[]> {
  return new Promise((resolve) => {
    const messages: ServerEnvelope[] = [];
    const timeout = setTimeout(() => resolve(messages), 50);
    socket.addEventListener('message', (event) => {
      messages.push(JSON.parse(event.data as string) as ServerEnvelope);
      if (messages.length === expectedCount) {
        clearTimeout(timeout);
        resolve(messages);
      }
    });
  });
}

function collectSnapshotExchange(
  socket: WebSocket,
  includesReady = false
): Promise<ServerEnvelope[]> {
  return new Promise((resolve) => {
    const messages: ServerEnvelope[] = [];
    const finish = (): void => {
      clearTimeout(timeout);
      socket.removeEventListener('message', onMessage);
      resolve([...messages]);
    };
    const onMessage = (event: MessageEvent): void => {
      const message = JSON.parse(event.data as string) as ServerEnvelope;
      messages.push(message);
      const chunks = messages.filter(({ type }) => type === 'snapshot') as SnapshotChunk[];
      if (
        chunks.length > 0
        && chunks[0].chunkCount !== undefined
        && chunks.length === chunks[0].chunkCount
        && (!includesReady || messages[0]?.type === 'session.ready')
      ) {
        finish();
      }
    };
    const timeout = setTimeout(finish, 250);
    socket.addEventListener('message', onMessage);
  });
}

function assertSnapshotChunks(
  messages: ServerEnvelope[],
  requestId: string | undefined,
  expectedLeaseCount: number,
  expectedPresenceCount = 0
): void {
  const chunks = messages as SnapshotChunk[];
  expect(chunks.length).toBeGreaterThan(1);
  const first = chunks[0];
  expect(first).toBeDefined();
  expect(chunks.map(({ type }) => type)).toEqual(chunks.map(() => 'snapshot'));
  expect(chunks.map(({ chunkIndex }) => chunkIndex)).toEqual(
    Array.from({ length: chunks.length }, (_, index) => index)
  );
  expect(chunks.every(({ chunkCount }) => chunkCount === chunks.length)).toBe(true);
  expect(chunks.every(({ snapshotId }) => snapshotId === first.snapshotId)).toBe(true);
  expect(chunks.every(({ stateVersion }) => stateVersion === first.stateVersion)).toBe(true);
  expect(chunks.every(({ serverTime }) => serverTime === first.serverTime)).toBe(true);
  expect(chunks.every((chunk) => chunk.requestId === requestId)).toBe(true);
  expect(chunks.every((chunk) => utf8Bytes(JSON.stringify(chunk)) <= MaximumEnvelopeBytes)).toBe(true);
  expect(chunks.flatMap(({ presence }) => presence)).toHaveLength(expectedPresenceCount);
  expect(chunks.flatMap(({ leases }) => leases)).toHaveLength(expectedLeaseCount);
}

function collectHeartbeatExchange(
  socket: WebSocket,
  expectedBroadcastCount: number
): Promise<ServerEnvelope[]> {
  return new Promise((resolve) => {
    const messages: ServerEnvelope[] = [];
    const finish = (): void => {
      clearTimeout(timeout);
      socket.removeEventListener('message', onMessage);
      resolve([...messages]);
    };
    const onMessage = (event: MessageEvent): void => {
      messages.push(JSON.parse(event.data as string) as ServerEnvelope);
      const first = messages[0] as SnapshotChunk | undefined;
      if (
        first?.type === 'snapshot'
        && messages.length === first.chunkCount + expectedBroadcastCount
      ) {
        finish();
      }
    };
    const timeout = setTimeout(finish, 250);
    socket.addEventListener('message', onMessage);
  });
}

async function seedReservations(
  developer: { developerId: string },
  count: number
): Promise<void> {
  await runInDurableObject(projectObject(), (_instance, state) => {
    for (let index = 0; index < count; index += 1) {
      const path = `assets/scenes/chunk-${index.toString().padStart(3, '0')}.unity`;
      state.storage.sql.exec(`
        INSERT INTO reservations (
          reservation_id, canonical_path, display_path, developer_id, display_name, branch, task,
          created_at, expires_at
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
      `,
      crypto.randomUUID(),
      path,
      path,
      developer.developerId,
      'Rin',
      `feature/${'b'.repeat(240)}`,
      't'.repeat(256),
      '2026-08-08T00:00:00.000Z',
      '2099-08-08T00:00:00.000Z');
    }
  });
}

function utf8Bytes(value: string): number {
  return new TextEncoder().encode(value).byteLength;
}

function collectClose(socket: WebSocket): Promise<CloseEvent | undefined> {
  return new Promise((resolve) => {
    const timeout = setTimeout(() => resolve(undefined), 50);
    socket.addEventListener('close', (event) => {
      clearTimeout(timeout);
      resolve(event);
    });
  });
}
