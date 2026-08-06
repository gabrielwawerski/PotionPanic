---
title: Coordinated File Leasing System
---

# Coordinated File Leasing System Implementation Plan

> **For agentic workers:** Implement this plan task by task. Keep each task
> independently testable and commit after its verification steps pass.

**Goal:** Add a remote coordination system that publishes Unity file presence,
claims files when editing starts, prevents accidental conflicting saves, and
keeps authoritative state in a Cloudflare Durable Object.

**Architecture:** The Unity editor extension and Cloudflare Worker remain in
this repository for the first release. One SQLite-backed Durable Object per
project owns sessions, connections, presence, leases, reservations, replay
records, and the monotonic coordination state version. The Unity editor client
uses Windows Credential Manager for the long-lived developer token, retains a
short-lived opaque session only in memory, and resumes a blocked save only after
an authoritative result arrives.

**Tech stack:** Unity `6000.5.1f1`, C# editor scripting, Unity Test Framework
EditMode tests, Windows Credential Manager, TypeScript, Cloudflare Workers,
SQLite-backed Durable Objects, WebSocket Hibernation API, Wrangler, and Vitest.

## Approved behavior

- Opening a coordinated file publishes non-exclusive `viewing` presence.
- The first meaningful modification requests an exclusive `editing` lease.
- A developer may reserve an unclaimed coordinated file before editing it.
- Initial coordinated files are all scenes and an explicit allowlist of
  important prefabs.
- A healthy client sends a heartbeat every 30 seconds.
- A disconnected connection's viewing presence and editing leases expire after
  120 seconds.
- Reservations last 30 minutes and remain valid when the owning connection
  closes.
- Closing an editing file releases its lease unless the developer reserves it.
- Editing a remotely claimed file shows a warning with owner, branch, task, and
  expiry. Saving requires cancellation or an explicit override.
- An override immediately transfers ownership and notifies the displaced
  developer.
- Backend outages never block local editing or permanently trap unsaved work.
- Important events initially use Unity notifications and a Coordination window.
  Native Windows notifications and Rider integration are deferred.
- The backend stores only current developers, sessions, connections, presence,
  leases, reservations, and short-lived replay records. It keeps no activity
  history.
- Authentication uses one revocable developer token per developer. The token is
  entered once per Windows user and machine, stored in Windows Credential
  Manager, and exchanged for an opaque 24-hour session.
- Machine-local task context, preferences, and local development endpoint stay
  outside Git. Repository-wide coordination rules live in tracked
  `coordination.json`.
- The system is advisory coordination, not a hard filesystem lock. Manual
  announcements remain required for protected changes during outages.

## Repository boundary and constraints

Keep the first release inside `gabrielwawerski/PotionPanic`:

```text
PotionPanic/
├── coordination.json
├── Assets/Scripts/Editor/Coordination/
├── Assets/Tests/EditMode/Coordination/
├── Tools/CoordinationServer/
└── Docs/
```

- Do not create a separate repository until another project uses the system,
  independent releases are required, or another team owns access control.
- Keep runtime gameplay assemblies independent of coordination code.
- Reuse `PotionPanic.Editor`; add `PotionPanic.Editor` to the existing
  `PotionPanic.EditModeTests` assembly references.
- Put Cloudflare code under `Tools/CoordinationServer` and commit its
  `package-lock.json`.
- Use 2-space indentation. Do not commit developer tokens, sessions,
  `.dev.vars`, local coordination settings, logs, caches, or runtime lease
  state.
- Windows is the only supported editor host in the first release. Guard
  Credential Manager P/Invoke code and show `Disabled` on unsupported hosts.
- Use a short-lived branch for each production implementation task. Do not mix
  this work with scene, prefab, package, or project-setting edits.

## Configuration contract

Create `coordination.json` at the repository root:

```json
{
  "schemaVersion": 1,
  "projectId": "potion-panic",
  "serverBaseUrl": "https://potion-panic-coordination.<account>.workers.dev",
  "heartbeatSeconds": 30,
  "rules": [
    {
      "pattern": "Assets/Scenes/**/*.unity",
      "mode": "exclusive",
      "claimOn": "dirty",
      "enabled": true
    }
  ]
}
```

- `serverBaseUrl` is the one committed endpoint. Derive the HTTP and WebSocket
  URLs from it.
- `heartbeatSeconds` is client behavior. Lease and reservation durations are
  server-authoritative and returned in `session.ready`.
- Define `**/` as matching zero or more directories, so the initial direct path
  `Assets/Scenes/SampleScene.unity` matches the scene rule.
- Add prefab rules only after confirming actual paths. Start with exact
  allowlisted paths, not `Assets/**/*.prefab`.
- Keep local Wrangler endpoint overrides in the untracked user settings file.

## Authenticated protocol

Use protocol version `1` and flat JSON envelopes that Unity can deserialize
without an additional JSON package. The endpoint and authenticated session,
not client-provided fields, determine project, developer, and connection.

```text
POST /v1/projects/{projectId}/sessions
Authorization: Bearer <developer-token>

GET /v1/projects/{projectId}/connect
Authorization: Bearer <session-token>
Upgrade: websocket
```

The session response returns the canonical developer ID and display name,
server time, assigned connection ID, `leaseTtlSeconds`,
`reservationTtlSeconds`, and the current `stateVersion`.

Client-to-server message types:

```text
presence.open
presence.close
lease.acquire
lease.release
lease.reserve
lease.override
heartbeat
snapshot.request
```

Server-to-client message types:

```text
session.ready
snapshot
presence.updated
presence.removed
lease.granted
lease.denied
lease.updated
lease.released
lease.overridden
error
```

Protocol invariants:

- Every message has `protocolVersion: 1`. Every mutating request has a UUID
  `requestId`.
- Server messages carrying state include the resulting monotonic
  `stateVersion`. Clients discard older state.
- Paths normalize separators and Unicode, reject control characters, leading
  separators, drive prefixes, `.` and `..` segments, and use a lower-invariant
  canonical key. The display path preserves the submitted normalized casing.
- The server derives project and developer identity from the authenticated
  request. `connectionId` is server-assigned and never accepted from a client
  envelope.
- One canonical path has at most one editing or reserved lease. Presence may
  contain multiple connections for one path.
- Replay records are scoped to developer and request ID, store a payload hash,
  and return the earlier result only for an identical payload for five minutes.
  A mismatched reuse fails explicitly.
- The server rejects messages over 16 KiB and task or branch context over the
  documented protocol limits.

## State and authentication model

- A developer token is generated server-side from 32 random bytes, displayed
  once, and persisted only as a domain-separated HMAC-SHA-256 digest.
- A session token is a random opaque value stored only as an HMAC digest with
  developer ID and 24-hour expiry. The client keeps it in memory only.
- The administration endpoint requires a separate random `ADMIN_TOKEN` secret.
  Revocation marks the developer revoked, deletes active session records, and
  closes that developer's active sockets without affecting other developers.
- A connection owns its presence records and editing leases. Only that
  connection's heartbeat extends its lease. Reconnecting as the same developer
  may rebind that developer's lease to the new connection; a stale connection
  cannot release or extend the rebound lease.
- A reservation is developer-owned and independent of a connection. It can be
  created from an unclaimed path, converted from that developer's editing lease,
  or converted back to editing by that developer's first dirty transition.
- The Durable Object schedules only its nearest expiry because it has one
  active alarm. Alarm processing deletes all due sessions, connections,
  presence, leases, reservations, and replay records, broadcasts resulting
  state changes, then schedules the next expiry.

## File map

### Repository and CI

- Create `coordination.json` and add scoped ignores for coordination secrets,
  local settings, logs, and caches.
- Modify `Assets/Tests/EditMode/PotionPanic.EditModeTests.asmdef` to reference
  `PotionPanic.Editor`.
- Create `.github/workflows/coordination-server.yml` to install, test, type
  check, and dry-run the backend without deployment credentials.

### Cloudflare service

Create `Tools/CoordinationServer/` with `package.json`, `package-lock.json`,
`tsconfig.json`, `wrangler.jsonc`, `vitest.config.ts`, `.dev.vars.example`,
`README.md`, `src/index.ts`, `src/env.ts`, `src/protocol.ts`,
`src/auth/crypto.ts`, `src/auth/session.ts`, `src/coordination-object.ts`,
`scripts/issue-token.mjs`, and focused protocol, authentication, lease, and
WebSocket tests.

### Unity editor client

Create focused files under `Assets/Scripts/Editor/Coordination` for models,
configuration, user settings, Credential Manager access, HTTP/WebSocket
clients, reconnection, service state, Git context, path matching, scene and
prefab tracking, save conflict processing, bootstrap, notifications, and the
Coordination window. Create matching tests under
`Assets/Tests/EditMode/Coordination`.

## Task 0: Restore the repository verification baseline

- [ ] Create `PP-8` for the stale Docboard package-wiring test that imports
  missing `Docs/.vitepress/project-docs.config.ts`.
- [ ] Fix that ticket independently before claiming the root `npm test` suite
  is clean. Do not bundle its implementation into coordination commits.
- [ ] Record the baseline failure in the coordination ticket until PP-8 closes.

## Task 1: Scaffold backend, configuration, and test access

- [ ] Create the server package, committed lockfile, strict TypeScript,
  Wrangler SQLite Durable Object migration, Vitest Workers integration, and
  CI workflow before running backend protocol tests.
- [ ] Add failing Unity tests proving the existing EditMode test assembly can
  reference coordination editor code, then add the assembly reference.
- [ ] Add failing tests for root and nested scene rule matching, disabled
  rules, one base URL, and local override precedence.
- [ ] Implement only the configuration DTOs, loader, matcher, and local user
  settings needed for those tests.
- [ ] Commit with `feat(coordination): scaffold backend and configuration`.

## Task 2: Define and validate the protocol

- [ ] Add failing TypeScript and Unity tests for protocol version rejection,
  message size, path canonicalization, direct-scene glob matching, invalid
  traversal, and stale `stateVersion` rejection.
- [ ] Implement flat message DTOs and validation. Derive project, developer,
  and connection values from authenticated server context rather than client
  payloads.
- [ ] Include server time, TTLs, assigned connection ID, and `stateVersion` in
  `session.ready`.
- [ ] Commit with `feat(coordination): define authenticated protocol`.

## Task 3: Implement opaque developer and session authentication

- [ ] Add failing tests for server-generated tokens, digest-only persistence,
  session expiry, invalid token rejection, revocation, and the requirement that
  tokens never appear in URLs or logs.
- [ ] Implement HMAC digesting with domain separation, opaque session storage,
  separate administrator authentication, and token issuance/revocation routes.
- [ ] Make revocation remove active sessions and close the revoked developer's
  sockets.
- [ ] Document issuance, rotation, and revocation in the server README.
- [ ] Commit with `feat(coordination): add revocable opaque sessions`.

## Task 4: Implement authoritative state and expiry

- [ ] Add failing concurrency tests for simultaneous acquire and reserve,
  connection-scoped heartbeats, stale presence expiry, replay payload mismatch,
  reservation conversion, rebinding, override, and nearest-expiry alarm
  scheduling.
- [ ] Create SQLite tables for developers, sessions, connections, presence,
  leases, reservations, replay records, and state version.
- [ ] Implement atomic transitions and one-alarm pruning. Keep no activity
  history.
- [ ] Commit with `feat(coordination): implement authoritative lease state`.

## Task 5: Add hibernating WebSocket synchronization

- [ ] Authenticate the WebSocket upgrade through the Authorization header and
  route project identity from the path.
- [ ] Attach only server-derived metadata using WebSocket hibernation
  attachments. Restore it after hibernation.
- [ ] Send an ordered snapshot immediately, broadcast state changes with their
  version, and remove stale connection state through normal expiry processing.
- [ ] Add reconnect, snapshot ordering, hibernation, duplicate-request, and
  active-revocation tests.
- [ ] Commit with `feat(coordination): synchronize authoritative state`.

## Task 6: Add secure Unity authentication and connection state

- [ ] Add failing EditMode tests using a mock credential store for token setup,
  identity returned by the server, forgotten credentials, session refresh, and
  unsupported-platform disabling.
- [ ] Implement `ICredentialStore` and Windows Credential Manager access under
  Windows editor compilation conditions. Store only the developer token under
  `PotionPanic/Coordination/<projectId>/developer-token`.
- [ ] Build HTTP and WebSocket clients that keep sessions in memory, marshal
  editor state to the main thread, and expose `Connected`, `Reconnecting`,
  `Offline`, `AuthenticationFailed`, and `Disabled`.
- [ ] Never queue lease mutations while offline.
- [ ] Commit with `feat(coordination): add secure Unity connection service`.

## Task 7: Track scenes and selected prefabs

- [ ] Use the installed Unity scene and Prefab Stage open, dirty, save, and
  close callbacks.
- [ ] Add tests for untitled and additive scenes, duplicate callbacks, domain
  reload, selected prefabs, non-coordinated prefabs, reconnect, and own
  reservation conversion on first dirty transition.
- [ ] Publish presence only for enabled rules and request one lease per dirty
  transition. Republish loaded stages after reconnect or domain reload.
- [ ] Commit scene and prefab tracking in independently reviewable commits.

## Task 8: Guard conflicting saves with cancel and resume

- [ ] Add failing tests for remote conflicts, pending claims, multi-path saves,
  offline save without claim, override failure, and recursive resume prevention.
- [ ] In `AssetModificationProcessor.OnWillSaveAssets`, return all safe paths
  immediately and omit conflicted paths. Start the asynchronous acquisition or
  override request after the callback returns.
- [ ] After an authoritative grant or override, resume only the omitted target
  save through `EditorApplication.delayCall` using a one-shot recursion guard.
- [ ] Preserve dirty local changes if the request fails or the editor reloads.
- [ ] Commit with `feat(coordination): guard conflicting saves`.

## Task 9: Build the coordination interface and harden lifecycle behavior

- [ ] Add `Window > Potion Panic > Coordination` with authenticated identity,
  branch/task context, presence, leases, reservations, connection state, and
  actions to reconnect, reserve, release, override, copy path, and forget
  credentials.
- [ ] Use Unity notifications only for claims, conflicts, overrides,
  reservations, authentication failure, and prolonged disconnect.
- [ ] Release owned connection presence and editing leases on normal shutdown
  where possible. Treat abrupt shutdown as stale expiry.
- [ ] Ensure compilation and domain reload recreate services without duplicate
  subscriptions.
- [ ] Commit with `feat(coordination): add editor coordination interface`.

## Task 10: Deploy and perform two-machine acceptance testing

- [ ] Create `TOKEN_HMAC_KEY` and `ADMIN_TOKEN` as Cloudflare secrets, deploy
  the Worker, and replace the endpoint placeholder in `coordination.json`.
- [ ] Issue one developer token per person and configure two Windows machines
  on different networks.
- [ ] Verify viewing presence, pre-edit reservation, simultaneous acquisition,
  remote conflict, override, clean close, process termination, network loss,
  server outage, token revocation, session refresh, and 120-second stale expiry.
- [ ] Run the complete coordination EditMode tests and a Play Mode smoke test
  against the scene that is canonical at execution time.
- [ ] Record backend, Unity, and two-machine evidence in `PP-7`.

## Task 11: Documentation and handoff

- [ ] Update `README.md`, onboarding, team workflow, and editor-safety guidance
  only after the accepted first release exists.
- [ ] Document token setup, reservations, overrides, offline recovery, advisory
  locking limits, and the manual-outage fallback.
- [ ] Keep manual announcements for protected changes. Do not describe the
  system as a hard lock or an automatic replacement for repository rules.
- [ ] Run `npm test`, `npm run docs:build`, backend checks, Unity EditMode
  tests, and Play Mode smoke tests. Do not claim the root test suite is clean
  until `PP-8` closes.
- [ ] Archive this plan only after release acceptance and documentation handoff.

## Verification and acceptance criteria

The first usable release is complete only when:

- Two developers connect from different networks using opaque sessions.
- Opening a coordinated scene or selected prefab publishes viewing presence.
- Pre-edit reservation and first dirty acquisition each produce exactly one
  authoritative owner.
- Presence, leases, reservations, branch context, task context, and expiry
  remain current after reconnect and hibernation.
- Saving against a remote owner requires a deliberate cancel or override path.
- The callback-based save guard preserves local work during pending, failed,
  offline, and domain-reload paths.
- Clean close releases editing state, while abrupt connection loss removes
  presence and editing leases within 120 seconds.
- Revocation immediately invalidates only the affected developer's sessions.
- No activity history, token, session, or generated runtime state enters Git.
- Backend tests, Unity tests, Play Mode smoke testing, documentation build, and
  two-machine acceptance evidence are recorded. The root documentation suite
  is claimed clean only after `PP-8` is resolved.

## Rollback

If the Unity client destabilizes the editor, enable the local `Disabled` switch,
stop networking and tracking, preserve project editability, and revert the
coordination client commits if required. If the backend fails, clients enter
offline mode and the team uses explicit manual announcements. Rotate developer
tokens only for credential exposure.
