# Coordinated File Leasing Stabilization Plan

## Summary

Pause Slice 06. Add two prerequisite slices:

- **05A:** Backend correctness, expiry, transactions, tests, and dependencies.
- **05B:** Unity protocol, transport, heartbeat, reconnect, and request correlation.

Update the master dependency graph to `05 → 05A → 05B → 06`. Record the current failed gates in PP-7 before implementation.

## Implementation Changes

### Slice 05A: Backend stabilization

- Replace fixed calendar timestamps in state tests with a future time derived from the test run.
- Wrap each multi-statement state transition, state-version increment, and replay insertion in `storage.transactionSync`. Schedule alarms and perform socket operations after commit.
- Extend state transitions with connection-closure directives. When a session expires:
    - Remove and close its socket with code `4001`.
    - Exclude it from later broadcasts.
    - Broadcast its presence and lease removals only to remaining connections.
- Preserve replayed responses as specified, but add tests covering replay after newer state exists.
- Upgrade:
    - `@cloudflare/vitest-pool-workers` to `0.20.3`
    - `wrangler` to `4.120.0`
    - Lock `miniflare` at `5.20260801.1-alpha` and `undici` at `7.29.0`
- Do not upgrade TypeScript, Node types, or unrelated packages.

Commit: `fix(coordination): stabilize authoritative backend`

### Slice 05B: Unity client stabilization

- Fix `lease.denied` parsing so an explicit `currentLease: null` is valid.
- Replace the WebSocket transport with:
    - Bounded multi-frame text-message assembly.
    - A 16 KiB cumulative limit.
    - Serialized sends.
    - Proper close and single-notification handling.
- Add automatic connection management:
    - Heartbeat at `coordination.json`’s interval while connected.
    - One active connection attempt at a time.
    - Network reconnect delays of 1, 2, 4, 8, 16, then 30 seconds with injectable jitter.
    - Immediate session recreation after close code `4001`.
    - No retry after revocation code `4003`, disabled state, credential removal, or explicit shutdown.
    - `ShutdownAsync` to cancel heartbeat/reconnect loops and close the socket.
- Remove the redundant snapshot request after `session.ready`; the server already sends an ordered snapshot.
- Replace boolean-only mutation methods with request-correlating APIs:
    - `CoordinationRequestHandle` contains request ID, type, and normalized path.
    - Each `Try*` method returns its handle through an `out` parameter.
    - Add request-completed and request-send-failed events.
    - Validate older replay responses and report them to the matching request without applying them as current state.
- Surface Credential Manager failures other than “not found” instead of prompting as if no credential existed.
- Remove old method overloads because no compatibility requirement or production caller exists.

Commit: `fix(coordination): harden Unity connection service`

### Correct Slices 06–08

- **Slice 06**
    - Publish presence for enabled rules.
    - Acquire editing leases only for `exclusive` rules.
    - On scene or Prefab Stage close, release presence and the owned editing lease. The server-side reservation then resurfaces automatically.
    - After reconnect or domain reload, inventory loaded stages, republish presence, and reacquire leases for stages already dirty.
    - Add a local authoritative state store used by the save guard.
    - Do not invent prefab paths. The current allowlist remains empty until real prefabs exist.

- **Slice 07**
    - Key pending saves by request ID and normalized path set.
    - Resume a save only when current state confirms that the local developer owns the editing lease. A stale replayed grant is insufficient.
    - Add `Save locally without coordination`, enabled only during an outage, reconnect, timeout, or transport-level override failure.
    - Require a second confirmation showing affected paths and the last known owner.
    - Mark the save as uncoordinated in memory, show it in the Coordination UI, and log a warning. Do not create backend history or tracked state.
    - Preserve dirty work for cancellation, authoritative denial, reload, and failed saves.

- **Slice 08**
    - Own service startup and `ShutdownAsync`.
    - Prevent duplicate bootstrap, heartbeat, reconnect, and event subscriptions across domain reload.
    - Display uncoordinated-save warnings until the affected asset closes or coordination confirms ownership.

## Test Plan

### Slice 05A gate

- Backend typecheck, auth, state, WebSocket, and full test suites pass.
- Tests cover:
    - Execution on arbitrary calendar dates.
    - Transaction rollback without partial lease deletion or version advancement.
    - Expired socket closure and broadcast exclusion.
    - Unaffected sockets remaining connected.
    - Duplicate replay after newer state.
- `npm audit --audit-level=moderate` reports zero vulnerabilities.
- Wrangler deployment dry run passes.

### Slice 05B gate

- Full Unity Coordination EditMode suite passes with no ignored failure.
- Add tests for:
    - Nullable `currentLease`.
    - Fragmented and exact-limit messages.
    - Oversized and binary messages.
    - Concurrent send ordering.
    - Heartbeat scheduling and cancellation.
    - Automatic reconnect, backoff, session expiry, revocation, and shutdown.
    - Request ID exposure, send failure, and stale replay handling.
- Unity compiles without new Console errors.

### Later-slice acceptance

- Closing an asset releases its editing lease while preserving its reservation.
- Dirty stages reacquire coordination after reload.
- Multi-path saves resume only authorized paths.
- Offline local save requires explicit confirmation and never clears unrelated dirty state.
- Run root tests, documentation build, backend gates, Unity EditMode tests, Play Mode smoke testing, and the existing two-machine Slice 09 acceptance.

## Assumptions

- Confirmed local saving during outages is intentional and advisory.
- Protocol v1 JSON remains unchanged; close code `4001` is transport behavior.
- Each stabilization slice is a separate reversible commit and PP-7 handoff.
- Slice 06 remains blocked until both stabilization gates are green.
