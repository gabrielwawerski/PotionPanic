---
title: 'Coordinated Leasing 04: Hibernating WebSocket Synchronization'
---

# Coordinated Leasing 04: Hibernating WebSocket Synchronization

**Session goal:** Expose the authoritative state through an authenticated, hibernation-safe WebSocket connection with ordered snapshots and broadcasts.

**Depends on:** Slice 03.

**Produces:** The stable server transport consumed by the Unity connection service.

## Files

- Modify `Tools/CoordinationServer/src/index.ts` and
  `src/coordination-object.ts`.
- Add transport tests under `Tools/CoordinationServer/tests/websocket/`.
- Update `Tools/CoordinationServer/README.md` with local WebSocket operation.

## Implementation steps

- Authenticate the upgrade from the `Authorization` header and derive the project from `/v1/projects/{projectId}/connect`. Reject query-string tokens.
- After authentication, call Slice 03's `openConnection(session)`, serialize the returned server-assigned connection metadata, and include that `connectionId`
  only in `session.ready`. An HTTP session response never creates a connection.
- Attach only server-derived project, developer, connection, and session metadata using WebSocket Hibernation attachments. Restore that metadata after hibernation by rebuilding the in-memory socket map from
  `this.ctx.getWebSockets()` and `deserializeAttachment()`. Reject altered client metadata.
- Send `session.ready` and an ordered `snapshot` immediately after upgrade, including server time, lease and reservation TTLs, assigned connection ID, and current state version.
- Route validated client messages to the Durable Object, return the replayed result for identical request IDs, and broadcast each resulting state change with its new version. Client requests do not include a state version; evaluate every mutation against current authoritative state. Clients discard older server state when applying responses.
- On developer revocation, close only that developer's active sockets, call
  `closeConnection` for each, and broadcast the resulting transitions. Let normal alarm processing call Slice 03's `pruneExpired(now)` and broadcast its returned transitions before scheduling the next expiry. Do not add a second expiry mechanism in the WebSocket layer.

## Verification

Run from `Tools/CoordinationServer`:

- `npm run typecheck`
- `npm test -- tests/websocket`
- `npm test`
- `npx wrangler deploy --dry-run`

Cover upgrade authentication, snapshot ordering, hibernation restore, reconnect, server-assigned connection IDs, duplicate requests, older server versions, oversized messages, and active revocation. Confirm the protocol test suite still passes.

**Commit:** `feat(coordination): synchronize authoritative state`

**Handoff:** Record the WebSocket endpoint contract and test evidence in `PP-7`. Slice 05 may now implement the Unity client against the deployed or local Worker without inventing a parallel protocol.
