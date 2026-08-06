---
title: 'Coordinated Leasing 04: Hibernating WebSocket Synchronization'
---

# Coordinated Leasing 04: Hibernating WebSocket Synchronization

**Session goal:** Expose the authoritative state through an authenticated,
hibernation-safe WebSocket connection with ordered snapshots and broadcasts.

**Depends on:** Slice 03.

**Produces:** The stable server transport consumed by the Unity connection
service.

## Files

- Modify `Tools/CoordinationServer/src/index.ts` and
  `src/coordination-object.ts`.
- Add transport tests under `Tools/CoordinationServer/tests/websocket/`.
- Update `Tools/CoordinationServer/README.md` with local WebSocket operation.

## Implementation steps

- Authenticate the upgrade from the `Authorization` header and derive the
  project from `/v1/projects/{projectId}/connect`. Reject query-string tokens.
- Attach only server-derived project, developer, connection, and session
  metadata using WebSocket Hibernation attachments. Restore that metadata after
  hibernation and reject altered client metadata.
- Send `session.ready` and an ordered `snapshot` immediately after upgrade,
  including server time, lease and reservation TTLs, assigned connection ID, and
  current state version.
- Route validated client messages to the Durable Object, return the replayed
  result for identical request IDs, and broadcast each resulting state change
  with its new version. Clients that send an older version receive current
  state rather than mutating stale state.
- Close revoked developer sockets and let normal alarm processing remove stale
  connection state. Do not add a second expiry mechanism in the WebSocket layer.

## Verification

Run from `Tools/CoordinationServer`:

- `npm run typecheck`
- `npm test -- tests/websocket`
- `npm test`
- `npx wrangler deploy --dry-run`

Cover upgrade authentication, snapshot ordering, hibernation restore,
reconnect, duplicate requests, stale versions, oversized messages, and active
revocation. Confirm the protocol test suite still passes.

**Commit:** `feat(coordination): synchronize authoritative state`

**Handoff:** Record the WebSocket endpoint contract and test evidence in `PP-7`.
Slice 05 may now implement the Unity client against the deployed or local
Worker without inventing a parallel protocol.
