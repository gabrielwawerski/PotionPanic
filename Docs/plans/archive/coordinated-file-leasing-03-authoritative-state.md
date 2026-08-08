---
title: 'Coordinated Leasing 03: Authoritative State and Expiry'
---

# Coordinated Leasing 03: Authoritative State and Expiry

**Session goal:** Implement the SQLite-backed Durable Object state machine that
authoritatively owns presence, editing leases, reservations, replay records, and
expiry.

**Depends on:** Slice 02.

**Produces:** Atomic state transitions and deterministic snapshots for the
WebSocket transport, with no socket ownership or broadcasting.

## Files

- Create or modify `Tools/CoordinationServer/src/coordination-object.ts`.
- Modify `Tools/CoordinationServer/src/protocol.ts` and `src/index.ts` only to
  retain authenticated session routing and expose no coordination-mutation HTTP
  endpoint.
- Add state tests under `Tools/CoordinationServer/tests/state/`.

## Implementation steps

- Extend the Slice 02 Durable Object schema with `connections`, `presence`,
  `leases`, `reservations`, and `replayRecords`. Reuse the existing `developers`,
  `sessions`, and state-version row. Store no activity history.
- Implement canonical-path uniqueness: one editing or reserved lease per path,
  with multiple presence rows allowed. Store owner, branch, task, connection,
  created time, expiry, and reservation status.
- Implement `presence.open`, `presence.close`, `lease.acquire`, `lease.release`,
  `lease.reserve`, `lease.override`, `heartbeat`, and `snapshot.request` as
  atomic transitions. Every state mutation increments the monotonic version.
- Expose `openConnection(session)` and `closeConnection(connectionId)` for Slice
  04. Each mutating operation returns a typed transition containing the requester
  response, zero or more state-change envelopes, and the resulting state version.
  It never sends a WebSocket message itself.
- Scope replay records by developer and request ID, store a payload hash, replay
  an identical result for five minutes, and explicitly reject a mismatched
  payload reuse.
- Make heartbeats extend only the connection's own presence and editing leases.
  Allow same-developer reconnect rebinding while preventing a stale connection
  from releasing or extending the rebound lease.
- Implement a pure `pruneExpired(now)` operation that prunes due sessions,
  connections, presence, leases, reservations, and replay records, returning
  the resulting state-change envelopes. Schedule only the nearest expiry alarm.
  In this slice, the alarm invokes pruning and schedules the next expiry without
  socket delivery; Slice 04 extends that alarm handler to broadcast the returned
  envelopes to live connections.

## Verification

Run from `Tools/CoordinationServer`:

- `npm run typecheck`
- `npm test -- tests/state`
- `npm test`

The focused tests must cover simultaneous acquire and reserve, connection-scoped
heartbeats, stale expiry, payload mismatch, reservation conversion, rebinding,
override, state-version monotonicity, connection creation and close, and
nearest-expiry alarm scheduling. Verify that no state transition depends on a
client-supplied state version or a WebSocket instance.

**Commit:** `feat(coordination): implement authoritative lease state`

**Handoff:** Record the schema assumptions, commit, and test output in `PP-7`.
Slice 04 must treat this object as the sole state owner and must not duplicate
lease logic in the transport. Slice 04 owns delivery of the returned envelopes
and must create connections only through `openConnection`.
