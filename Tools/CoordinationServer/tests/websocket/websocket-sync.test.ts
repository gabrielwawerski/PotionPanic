import {
  SELF,
  env,
  evictDurableObject,
  reset,
  runDurableObjectAlarm,
  runInDurableObject
} from 'cloudflare:test';
import { afterEach, describe, expect, it } from 'vitest';
import { VersionedServerState, type ServerEnvelope } from '../../src/protocol';

const projectId = 'potion-panic';
const projectUrl = `https://example.test/v1/projects/${projectId}`;
const adminToken = 'test-admin-token';

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
    expect(messages[1]).toMatchObject({ presence: [], leases: [] });
    expect(messages[1].stateVersion).toBe(messages[0].stateVersion);
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
        "UPDATE connections SET expires_at = '2000-01-01T00:00:00.000Z'"
      );
      state.storage.sql.exec(
        "UPDATE presence SET expires_at = '2000-01-01T00:00:00.000Z'"
      );
    });
    const solExpiry = collectMessages(solSocket, 1);

    expect(await runDurableObjectAlarm(projectObject())).toBe(true);
    expect(await solExpiry).toEqual([expect.objectContaining({
      type: 'presence.removed',
      connectionId: rinReady.connectionId
    })]);
  });

  it('rejects older server state when a client has already applied a newer version', () => {
    const state = new VersionedServerState();
    const newest: ServerEnvelope = {
      protocolVersion: 1,
      type: 'snapshot',
      stateVersion: 2,
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

function collectClose(socket: WebSocket): Promise<CloseEvent | undefined> {
  return new Promise((resolve) => {
    const timeout = setTimeout(() => resolve(undefined), 50);
    socket.addEventListener('close', (event) => {
      clearTimeout(timeout);
      resolve(event);
    });
  });
}
