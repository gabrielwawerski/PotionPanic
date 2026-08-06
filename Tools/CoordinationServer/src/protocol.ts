export const ProtocolVersion = 1 as const;
export const MaximumEnvelopeBytes = 16 * 1024;
export const MaximumPathLength = 1024;
export const MaximumContextLength = 256;

export const ClientMessageTypes = [
  'presence.open',
  'presence.close',
  'lease.acquire',
  'lease.release',
  'lease.reserve',
  'lease.override',
  'heartbeat',
  'snapshot.request'
] as const;

export const ServerMessageTypes = [
  'session.ready',
  'snapshot',
  'presence.updated',
  'presence.removed',
  'lease.granted',
  'lease.denied',
  'lease.updated',
  'lease.released',
  'lease.overridden',
  'error'
] as const;

export type ClientMessageType = typeof ClientMessageTypes[number];
export type ServerMessageType = typeof ServerMessageTypes[number];

export interface ClientEnvelopeBase {
  protocolVersion: typeof ProtocolVersion;
  type: ClientMessageType;
  requestId: string;
}

export interface PathContextEnvelope extends ClientEnvelopeBase {
  path: string;
  branch: string;
  task: string;
}

export interface PathEnvelope extends ClientEnvelopeBase {
  path: string;
}

export type ClientEnvelope =
  | (PathContextEnvelope & { type: 'presence.open' })
  | (PathEnvelope & { type: 'presence.close' })
  | (PathContextEnvelope & { type: 'lease.acquire' })
  | (PathEnvelope & { type: 'lease.release' })
  | (PathContextEnvelope & { type: 'lease.reserve' })
  | (PathContextEnvelope & { type: 'lease.override' })
  | (ClientEnvelopeBase & { type: 'heartbeat' })
  | (ClientEnvelopeBase & { type: 'snapshot.request' });

export interface PresenceRecord {
  path: string;
  displayPath: string;
  developerId: string;
  displayName: string;
  connectionId: string;
  branch: string;
  task: string;
  expiresAt: string;
}

export interface LeaseRecord {
  leaseId: string;
  path: string;
  displayPath: string;
  mode: 'editing' | 'reserved';
  developerId: string;
  displayName: string;
  branch: string;
  task: string;
  expiresAt: string;
  connectionId?: string;
}

export interface ServerEnvelopeBase {
  protocolVersion: typeof ProtocolVersion;
  type: ServerMessageType;
  stateVersion: number;
  requestId?: string;
}

export type ServerEnvelope =
  | (ServerEnvelopeBase & {
    type: 'session.ready'; developerId: string; displayName: string; serverTime: string;
    connectionId: string; leaseTtlSeconds: number; reservationTtlSeconds: number;
  })
  | (ServerEnvelopeBase & {
    type: 'snapshot'; presence: PresenceRecord[]; leases: LeaseRecord[]; serverTime: string;
  })
  | (ServerEnvelopeBase & { type: 'presence.updated'; presence: PresenceRecord[] })
  | (ServerEnvelopeBase & { type: 'presence.removed'; path: string; connectionId: string })
  | (ServerEnvelopeBase & { type: 'lease.granted'; path: string; lease: LeaseRecord })
  | (ServerEnvelopeBase & {
    type: 'lease.denied'; path: string; code: string; currentLease: LeaseRecord | null;
  })
  | (ServerEnvelopeBase & { type: 'lease.updated'; lease: LeaseRecord })
  | (ServerEnvelopeBase & { type: 'lease.released'; path: string; leaseId: string })
  | (ServerEnvelopeBase & {
    type: 'lease.overridden'; path: string; previousDeveloperId: string; lease: LeaseRecord;
  })
  | (ServerEnvelopeBase & { type: 'error'; code: string; message: string });

export type ProtocolValidationResult<T> =
  | { ok: true; value: T }
  | { ok: false; error: string };

export function parseClientEnvelope(input: unknown): ProtocolValidationResult<ClientEnvelope> {
  if (typeof input === 'string' && new TextEncoder().encode(input).byteLength > MaximumEnvelopeBytes) {
    return fail('envelope_too_large');
  }

  const parsed = parseInput(input);
  if (!parsed.ok) {
    return parsed;
  }

  const value = parsed.value;
  if (!isRecord(value) || hasIdentityField(value)) {
    return fail('invalid_envelope');
  }

  if (value.protocolVersion !== ProtocolVersion || !isClientMessageType(value.type)
    || !isUuidV4(value.requestId)) {
    return fail('invalid_envelope');
  }

  const base: ClientEnvelopeBase = {
    protocolVersion: ProtocolVersion,
    type: value.type,
    requestId: value.requestId
  };
  if (value.type === 'heartbeat' || value.type === 'snapshot.request') {
    return { ok: true, value: { ...base, type: value.type } };
  }

  const path = normalizePath(value.path);
  if (path === null) {
    return fail('invalid_path');
  }

  if (value.type === 'presence.close' || value.type === 'lease.release') {
    return { ok: true, value: { ...base, type: value.type, path } };
  }

  if (!isContext(value.branch) || !isContext(value.task)) {
    return fail('invalid_context');
  }

  return {
    ok: true,
    value: {
      ...base,
      type: value.type,
      path,
      branch: value.branch,
      task: value.task
    }
  };
}

export function parseServerEnvelope(input: unknown): ProtocolValidationResult<ServerEnvelope> {
  if (typeof input === 'string' && new TextEncoder().encode(input).byteLength > MaximumEnvelopeBytes) {
    return fail('envelope_too_large');
  }

  const parsed = parseInput(input);
  if (!parsed.ok) {
    return parsed;
  }

  const value = parsed.value;
  if (!isRecord(value) || value.protocolVersion !== ProtocolVersion
    || !isServerMessageType(value.type) || !isStateVersion(value.stateVersion)
    || (value.requestId !== undefined && !isUuidV4(value.requestId))) {
    return fail('invalid_envelope');
  }

  const base: ServerEnvelopeBase = {
    protocolVersion: ProtocolVersion,
    type: value.type,
    stateVersion: value.stateVersion,
    ...(value.requestId === undefined ? {} : { requestId: value.requestId })
  };
  switch (value.type) {
    case 'session.ready':
      return hasStrings(value, ['developerId', 'displayName', 'serverTime', 'connectionId'])
        && isPositiveInteger(value.leaseTtlSeconds) && isPositiveInteger(value.reservationTtlSeconds)
        ? success<ServerEnvelope>({ ...base, type: value.type, developerId: value.developerId,
          displayName: value.displayName, serverTime: value.serverTime,
          connectionId: value.connectionId, leaseTtlSeconds: value.leaseTtlSeconds,
          reservationTtlSeconds: value.reservationTtlSeconds } as ServerEnvelope) : fail('invalid_envelope');
    case 'snapshot':
      return Array.isArray(value.presence) && value.presence.every(isPresenceRecord)
        && Array.isArray(value.leases) && value.leases.every(isLeaseRecord) && isString(value.serverTime)
        ? success<ServerEnvelope>({ ...base, type: value.type, presence: value.presence as PresenceRecord[],
          leases: value.leases as LeaseRecord[], serverTime: value.serverTime } as ServerEnvelope) : fail('invalid_envelope');
    case 'presence.updated':
      return Array.isArray(value.presence) && value.presence.every(isPresenceRecord)
        ? success<ServerEnvelope>({ ...base, type: value.type, presence: value.presence as PresenceRecord[] } as ServerEnvelope)
        : fail('invalid_envelope');
    case 'presence.removed':
      return hasStrings(value, ['path', 'connectionId'])
        ? success<ServerEnvelope>({ ...base, type: value.type, path: value.path, connectionId: value.connectionId } as ServerEnvelope)
        : fail('invalid_envelope');
    case 'lease.granted':
      return isString(value.path) && isLeaseRecord(value.lease)
        ? success<ServerEnvelope>({ ...base, type: value.type, path: value.path, lease: value.lease as unknown as LeaseRecord } as ServerEnvelope)
        : fail('invalid_envelope');
    case 'lease.denied':
      return hasStrings(value, ['path', 'code'])
        && (value.currentLease === null || isLeaseRecord(value.currentLease))
        ? success<ServerEnvelope>({ ...base, type: value.type, path: value.path, code: value.code,
          currentLease: value.currentLease as unknown as LeaseRecord | null } as ServerEnvelope) : fail('invalid_envelope');
    case 'lease.updated':
      return isLeaseRecord(value.lease)
        ? success<ServerEnvelope>({ ...base, type: value.type, lease: value.lease as unknown as LeaseRecord } as ServerEnvelope)
        : fail('invalid_envelope');
    case 'lease.released':
      return hasStrings(value, ['path', 'leaseId'])
        ? success<ServerEnvelope>({ ...base, type: value.type, path: value.path, leaseId: value.leaseId } as ServerEnvelope)
        : fail('invalid_envelope');
    case 'lease.overridden':
      return hasStrings(value, ['path', 'previousDeveloperId']) && isLeaseRecord(value.lease)
        ? success<ServerEnvelope>({ ...base, type: value.type, path: value.path,
          previousDeveloperId: value.previousDeveloperId, lease: value.lease as unknown as LeaseRecord } as ServerEnvelope)
        : fail('invalid_envelope');
    case 'error':
      return hasStrings(value, ['code', 'message'])
        ? success<ServerEnvelope>({ ...base, type: value.type, code: value.code, message: value.message } as ServerEnvelope)
        : fail('invalid_envelope');
  }
}

export class VersionedServerState {
  newestAppliedStateVersion = 0;

  tryApply(result: ProtocolValidationResult<ServerEnvelope>): boolean {
    if (!result.ok || result.value.stateVersion < this.newestAppliedStateVersion) {
      return false;
    }

    this.newestAppliedStateVersion = result.value.stateVersion;
    return true;
  }
}

export function normalizePath(path: unknown): string | null {
  if (!isString(path) || path.length === 0 || path.length > MaximumPathLength) {
    return null;
  }

  const normalized = path.normalize('NFC').replaceAll('\\', '/');
  if (normalized.startsWith('/') || /^[A-Za-z]:/.test(normalized)
    || /[\u0000-\u001F\u007F-\u009F]/.test(normalized)) {
    return null;
  }

  const segments = normalized.split('/').filter((segment) => segment.length > 0);
  if (segments.some((segment) => segment === '.' || segment === '..')) {
    return null;
  }

  return segments.join('/');
}

function parseInput(input: unknown): ProtocolValidationResult<unknown> {
  if (typeof input !== 'string') {
    return { ok: true, value: input };
  }

  try {
    return { ok: true, value: JSON.parse(input) as unknown };
  } catch {
    return fail('invalid_json');
  }
}

function isClientMessageType(value: unknown): value is ClientMessageType {
  return typeof value === 'string' && (ClientMessageTypes as readonly string[]).includes(value);
}

function isServerMessageType(value: unknown): value is ServerMessageType {
  return typeof value === 'string' && (ServerMessageTypes as readonly string[]).includes(value);
}

function hasIdentityField(value: Record<string, unknown>): boolean {
  return ['projectId', 'developerId', 'connectionId'].some((field) => field in value);
}

function isUuidV4(value: unknown): value is string {
  return typeof value === 'string'
    && /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function isContext(value: unknown): value is string {
  return isString(value) && value.length <= MaximumContextLength;
}

function isStateVersion(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= 0;
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isString(value: unknown): value is string {
  return typeof value === 'string';
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function hasStrings(value: Record<string, unknown>, fields: string[]): boolean {
  return fields.every((field) => isString(value[field]));
}

function isPresenceRecord(value: unknown): value is PresenceRecord {
  return isRecord(value) && hasStrings(value, [
    'path', 'displayPath', 'developerId', 'displayName', 'connectionId', 'branch', 'task', 'expiresAt'
  ]);
}

function isLeaseRecord(value: unknown): value is LeaseRecord {
  if (!isRecord(value) || !hasStrings(value, [
    'leaseId', 'path', 'displayPath', 'developerId', 'displayName', 'branch', 'task', 'expiresAt'
  ]) || (value.mode !== 'editing' && value.mode !== 'reserved')) {
    return false;
  }

  return value.mode === 'editing' ? isString(value.connectionId) : value.connectionId === undefined;
}

function success<T>(value: T): ProtocolValidationResult<T> {
  return { ok: true, value };
}

function fail<T = never>(error: string): ProtocolValidationResult<T> {
  return { ok: false, error };
}
