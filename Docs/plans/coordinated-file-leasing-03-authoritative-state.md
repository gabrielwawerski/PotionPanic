---
title: 'Coordinated Leasing 03: Authoritative State and Expiry'
---

# Coordinated Leasing 03: Authoritative State and Expiry

**Session goal:** Implement the SQLite-backed Durable Object state machine that
authoritatively owns presence, editing leases, reservations, replay records, and
expiry.

**Depends on:** Slice 02.

**Produces:** Atomic state transitions and deterministic snapshots for the
WebSocket transport.

## Files

- Create or modify `Tools/CoordinationServer/src/coordination-object.ts`.
- Modify `Tools/CoordinationServer/src/protocol.ts` and `src/index.ts` only to
  route authenticated requests into the object.
- Add state tests under `Tools/CoordinationServer/tests/state/`.

## Implementation steps

- Create SQLite tables for developers, sessions, connections, presence, leases,
  reservations, replay records, and one state-version row. Store no activity
  history.
- Implement canonical-path uniqueness: one editing or reserved lease per path,
  with multiple presence rows allowed. Store owner, branch, task, connection,
  created time, expiry, and reservation status.
- Implement `presence.open`, `presence.close`, `lease.acquire`, `lease.release`,
  `lease.reserve`, `lease.override`, `heartbeat`, and `snapshot.request` as
  atomic transitions. Every state mutation increments the monotonic version.
- Scope replay records by developer and request ID, store a payload hash, replay
  an identical result for five minutes, and explicitly reject a mismatched
  payload reuse.
- Make heartbeats extend only the connection's own presence and editing leases.
  Allow same-developer reconnect rebinding while preventing a stale connection
  from releasing or extending the rebound lease.
- Schedule only the nearest expiry alarm. Prune due sessions, connections,
  presence, leases, reservations, and replay records, broadcast resulting state
  changes, and schedule the next expiry.

## Verification

Run from `Tools/CoordinationServer`:

- `npm run typecheck`
- `npm test -- tests/state`
- `npm test`

The focused tests must cover simultaneous acquire and reserve, connection-scoped
heartbeats, stale expiry, payload mismatch, reservation conversion, rebinding,
override, state-version monotonicity, and nearest-expiry alarm scheduling.

**Commit:** `feat(coordination): implement authoritative lease state`

**Handoff:** Record the schema assumptions, commit, and test output in `PP-7`.
Slice 04 must treat this object as the sole state owner and must not duplicate
lease logic in the transport.
