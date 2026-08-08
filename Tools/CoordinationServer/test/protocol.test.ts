import {
  ClientMessageTypes,
  ServerMessageTypes,
  VersionedServerState,
  canonicalPathKey,
  parseClientEnvelope,
  parseServerEnvelope
} from '../src/protocol';
import { describe, expect, it } from 'vitest';

const requestId = '123e4567-e89b-42d3-a456-426614174000';
const snapshotId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const lease = {
  leaseId: 'lease-1',
  path: 'assets/scenes/a.unity',
  displayPath: 'Assets/Scenes/A.unity',
  mode: 'editing',
  developerId: 'dev-1',
  displayName: 'Rin',
  branch: 'feature/coordination',
  task: 'PP-7',
  expiresAt: '2026-08-06T00:02:00Z',
  connectionId: 'conn-1'
};

const presence = {
  path: 'assets/scenes/a.unity',
  displayPath: 'Assets/Scenes/A.unity',
  developerId: 'dev-1',
  displayName: 'Rin',
  connectionId: 'conn-1',
  branch: 'feature/coordination',
  task: 'PP-7',
  expiresAt: '2026-08-06T00:02:00Z'
};

describe('protocol v1 client envelopes', () => {
  it.each([
    ['Assets\\Scenes\\Ä\\İ.unity', 'assets/scenes/Ä/İ.unity'],
    ['ASSETS/MiXeD.unity', 'assets/mixed.unity'],
    ['Assets/Scenes/Cafe\u0301.unity', 'assets/scenes/café.unity'],
    ['Assets/Scenes/Café.unity', 'assets/scenes/café.unity']
  ])('creates NFC, slash-normalized, ASCII-folded canonical keys for %s', (path, expected) => {
    expect(canonicalPathKey(path)).toBe(expected);
  });

  it('lists every client and server message defined by the v1 contract', () => {
    expect(ClientMessageTypes).toEqual([
      'presence.open', 'presence.close', 'lease.acquire', 'lease.release',
      'lease.reserve', 'lease.override', 'heartbeat', 'snapshot.request'
    ]);
    expect(ServerMessageTypes).toEqual([
      'session.ready', 'snapshot', 'presence.updated', 'presence.removed',
      'lease.granted', 'lease.denied', 'lease.updated', 'lease.released',
      'lease.overridden', 'error'
    ]);
  });

  it('normalizes a valid lease acquire envelope', () => {
    const result = parseClientEnvelope(JSON.stringify({
      protocolVersion: 1,
      type: 'lease.acquire',
      requestId,
      path: 'Assets\\Scenes\\SampleScene.unity',
      branch: 'feature/coordination',
      task: 'PP-7'
    }));

    expect(result).toEqual({
      ok: true,
      value: {
        protocolVersion: 1,
        type: 'lease.acquire',
        requestId,
        path: 'Assets/Scenes/SampleScene.unity',
        branch: 'feature/coordination',
        task: 'PP-7'
      }
    });
  });

  it.each([
    { protocolVersion: 2, type: 'heartbeat', requestId },
    { protocolVersion: 1, type: 'heartbeat', requestId: 'not-a-uuid' },
    { protocolVersion: 1, type: 'lease.acquire', requestId, path: '../secret', branch: '', task: '' },
    { protocolVersion: 1, type: 'heartbeat', requestId, developerId: 'forbidden' }
  ])('rejects invalid client envelope %#', (value) => {
    expect(parseClientEnvelope(JSON.stringify(value))).toEqual(
      expect.objectContaining({ ok: false })
    );
  });

  it('rejects an envelope over 16 KiB', () => {
    const json = JSON.stringify({ protocolVersion: 1, type: 'heartbeat', requestId,
      padding: 'x'.repeat(16 * 1024) });

    expect(parseClientEnvelope(json)).toEqual(expect.objectContaining({ ok: false }));
  });

  it.each([
    { type: 'presence.open', path: 'Assets/Scenes/A.unity', branch: 'feature/a', task: 'PP-7' },
    { type: 'presence.close', path: 'Assets/Scenes/A.unity' },
    { type: 'lease.acquire', path: 'Assets/Scenes/A.unity', branch: 'feature/a', task: 'PP-7' },
    { type: 'lease.release', path: 'Assets/Scenes/A.unity' },
    { type: 'lease.reserve', path: 'Assets/Scenes/A.unity', branch: 'feature/a', task: 'PP-7' },
    { type: 'lease.override', path: 'Assets/Scenes/A.unity', branch: 'feature/a', task: 'PP-7' },
    { type: 'heartbeat' },
    { type: 'snapshot.request' }
  ])('accepts every v1 client message DTO', (value) => {
    expect(parseClientEnvelope(JSON.stringify({ protocolVersion: 1, requestId, ...value })))
      .toEqual(expect.objectContaining({ ok: true }));
  });

  it.each([
    { type: 'presence.open', branch: 'feature/a', task: 'PP-7' },
    { type: 'presence.close' },
    { type: 'lease.acquire', path: 'Assets/Scenes/A.unity', task: 'PP-7' },
    { type: 'lease.release' },
    { type: 'lease.reserve', path: 'Assets/Scenes/A.unity', branch: 'feature/a' },
    { type: 'lease.override', path: 'Assets/Scenes/A.unity', branch: 'feature/a' }
  ])('rejects a client DTO with a required field missing', (value) => {
    expect(parseClientEnvelope(JSON.stringify({ protocolVersion: 1, requestId, ...value })))
      .toEqual(expect.objectContaining({ ok: false }));
  });

  it('rejects a submitted path longer than 1,024 UTF-16 code units before normalization', () => {
    const path = `Assets/${'/'.repeat(1020)}A.unity`;

    expect(parseClientEnvelope(JSON.stringify({ protocolVersion: 1, type: 'lease.release',
      requestId, path }))).toEqual(expect.objectContaining({ ok: false }));
  });
});

describe('protocol v1 server envelopes', () => {
  it.each([
    [{ protocolVersion: 1, type: 'session.ready', stateVersion: 1,
      developerId: 'dev-1', displayName: 'Rin', serverTime: '2026-08-06T00:00:00Z',
      connectionId: 'conn-1', leaseTtlSeconds: 120, reservationTtlSeconds: 1800 }],
    [{ protocolVersion: 1, type: 'snapshot', stateVersion: 1, snapshotId, chunkIndex: 0,
      chunkCount: 1, presence: [presence], leases: [lease], serverTime: '2026-08-06T00:00:00Z' }],
    [{ protocolVersion: 1, type: 'presence.updated', stateVersion: 1, presence: [presence] }],
    [{ protocolVersion: 1, type: 'presence.removed', stateVersion: 1, path: 'Assets/Scenes/A.unity',
      connectionId: 'conn-1' }],
    [{ protocolVersion: 1, type: 'lease.granted', stateVersion: 1, path: 'Assets/Scenes/A.unity',
      lease }],
    [{ protocolVersion: 1, type: 'lease.denied', stateVersion: 1, path: 'Assets/Scenes/A.unity',
      code: 'already_leased', currentLease: null }],
    [{ protocolVersion: 1, type: 'lease.updated', stateVersion: 1, lease }],
    [{ protocolVersion: 1, type: 'lease.released', stateVersion: 1, path: 'Assets/Scenes/A.unity',
      leaseId: 'lease-1' }],
    [{ protocolVersion: 1, type: 'lease.overridden', stateVersion: 1,
      path: 'Assets/Scenes/A.unity', previousDeveloperId: 'dev-1', lease }],
    [{ protocolVersion: 1, type: 'error', stateVersion: 1, code: 'invalid_path', message: 'Bad path.' }]
  ])('accepts every v1 server message DTO', (value) => {
    expect(parseServerEnvelope(value)).toEqual(expect.objectContaining({ ok: true }));
  });

  it('rejects a reserved lease that carries a connection ID', () => {
    expect(parseServerEnvelope({
      protocolVersion: 1,
      type: 'snapshot',
      stateVersion: 1,
      snapshotId,
      chunkIndex: 0,
      chunkCount: 1,
      presence: [],
      leases: [{ ...lease, mode: 'reserved' }],
      serverTime: '2026-08-06T00:00:00Z'
    })).toEqual(expect.objectContaining({ ok: false }));
  });

  it.each([
    { snapshotId: 'not-a-uuid', chunkIndex: 0, chunkCount: 1 },
    { snapshotId, chunkIndex: -1, chunkCount: 1 },
    { snapshotId, chunkIndex: 0, chunkCount: 0 },
    { snapshotId, chunkIndex: 1, chunkCount: 1 }
  ])('rejects invalid snapshot chunk metadata', (chunk) => {
    expect(parseServerEnvelope({
      protocolVersion: 1,
      type: 'snapshot',
      stateVersion: 1,
      ...chunk,
      presence: [],
      leases: [],
      serverTime: '2026-08-06T00:00:00Z'
    })).toEqual(expect.objectContaining({ ok: false }));
  });

  it.each([
    { protocolVersion: 1, type: 'session.ready', stateVersion: 1 },
    { protocolVersion: 1, type: 'snapshot', stateVersion: 1, presence: [], leases: [] },
    { protocolVersion: 1, type: 'presence.updated', stateVersion: 1 },
    { protocolVersion: 1, type: 'presence.removed', stateVersion: 1, path: 'Assets/A.unity' },
    { protocolVersion: 1, type: 'lease.granted', stateVersion: 1, path: 'Assets/A.unity' },
    { protocolVersion: 1, type: 'lease.denied', stateVersion: 1, path: 'Assets/A.unity', code: 'denied' },
    { protocolVersion: 1, type: 'lease.updated', stateVersion: 1 },
    { protocolVersion: 1, type: 'lease.released', stateVersion: 1, path: 'Assets/A.unity' },
    { protocolVersion: 1, type: 'lease.overridden', stateVersion: 1, path: 'Assets/A.unity',
      previousDeveloperId: 'dev-1' },
    { protocolVersion: 1, type: 'error', stateVersion: 1, code: 'invalid_path' }
  ])('rejects a server DTO with a required field missing', (value) => {
    expect(parseServerEnvelope(value)).toEqual(expect.objectContaining({ ok: false }));
  });

  it('requires state version and ignores state older than the applied version', () => {
    const state = new VersionedServerState();
    const newest = parseServerEnvelope({
      protocolVersion: 1,
      type: 'snapshot',
      stateVersion: 3,
      snapshotId,
      chunkIndex: 0,
      chunkCount: 1,
      presence: [],
      leases: [],
      serverTime: '2026-08-06T00:00:00.000Z'
    });
    const older = parseServerEnvelope({
      protocolVersion: 1,
      type: 'snapshot',
      stateVersion: 2,
      snapshotId,
      chunkIndex: 0,
      chunkCount: 1,
      presence: [],
      leases: [],
      serverTime: '2026-08-06T00:00:00.000Z'
    });

    expect(newest).toEqual(expect.objectContaining({ ok: true }));
    expect(older).toEqual(expect.objectContaining({ ok: true }));
    expect(state.tryApply(newest)).toBe(true);
    expect(state.tryApply(older)).toBe(false);
    expect(state.newestAppliedStateVersion).toBe(3);
  });

  it('rejects a server envelope without a valid state version', () => {
    expect(parseServerEnvelope({ protocolVersion: 1, type: 'snapshot' })).toEqual(
      expect.objectContaining({ ok: false })
    );
  });

  it.each([
    JSON.stringify({ protocolVersion: 1, type: 'snapshot', stateVersion: 1,
      snapshotId, chunkIndex: 0, chunkCount: 1, presence: [], leases: [],
      serverTime: 'x'.repeat(16 * 1024) }),
    JSON.stringify({ protocolVersion: 1, type: 'error', stateVersion: 1,
      code: 'invalid_path', message: 'x'.repeat(16 * 1024) })
  ])('rejects a server envelope over 16 KiB', (json) => {
    expect(parseServerEnvelope(json)).toEqual(expect.objectContaining({ ok: false }));
  });
});
