# Coordinated File Leasing System Implementation Plan

> **For agentic workers:** Implement this plan task by task. Keep each task independently testable and commit after its verification steps pass.

**Goal:** Add a remote coordination system that shows when either Potion Panic developer has a coordinated Unity file open, claims it on first modification, warns about conflicting edits, and synchronizes state through a free Cloudflare backend.

**Architecture:** Keep the Unity editor client and Cloudflare service in this repository for the first version. A SQLite-backed Cloudflare Durable Object owns authoritative presence and lease state and broadcasts changes over WebSockets. The Unity editor extension detects scene and selected-prefab lifecycle events, stores a long-lived developer token in Windows Credential Manager, exchanges it for a 24-hour session, and presents coordination state inside Unity.

**Tech stack:** Unity `6000.5.1f1`, C# editor scripting, Unity Test Framework EditMode tests, Windows Credential Manager, TypeScript, Cloudflare Workers, SQLite-backed Durable Objects, WebSocket Hibernation API, Wrangler, and Vitest.

## Approved behavior

- Opening a coordinated file publishes non-exclusive `viewing` presence.
- The first meaningful modification requests an exclusive `editing` lease.
- Initial coordinated files are all scenes and an explicit allowlist of important prefabs.
- Project settings and package files are added only after scene and prefab handling is stable.
- A healthy client sends a heartbeat every 30 seconds.
- A disconnected editing lease expires after 120 seconds.
- Closing a file releases its lease unless the developer creates a 30-minute reservation.
- Editing a remotely claimed file shows a warning with owner, branch, task, and expiry.
- Saving a remotely claimed file requires an explicit override or cancellation.
- An override immediately transfers ownership and notifies the displaced developer.
- Backend outages never block local editing or permanently trap unsaved work.
- Important events initially use Unity notifications and a dedicated Coordination window.
- Native Windows notifications are deferred until the Unity-only workflow is proven.
- Rider integration is deferred.
- The backend stores only current users, sessions, presence, and leases. It keeps no activity history.
- Authentication uses one revocable developer token per person.
- The token is entered once per machine and stored in Windows Credential Manager.
- Machine-local identity, task context, and preferences remain outside Git.
- Repository-wide coordination rules live in tracked `coordination.json`.

## Repository boundary

Keep the first version inside `gabrielwawerski/PotionPanic`:

```text
PotionPanic/
├── coordination.json
├── Assets/
│   ├── Scripts/
│   │   └── Editor/
│   │       └── Coordination/
│   └── Tests/
│       └── EditMode/
│           └── Coordination/
├── Tools/
│   └── CoordinationServer/
└── Docs/
    └── plans/
```

Do not create a separate repository until another project uses the system, the client and server require independent releases, or a separate team owns deployment and access control.

## Global constraints

- Keep runtime gameplay assemblies independent of coordination code.
- Put all Unity client code under `Assets/Scripts/Editor/Coordination`.
- Put Unity tests under `Assets/Tests/EditMode/Coordination`.
- Put the Cloudflare service under `Tools/CoordinationServer`.
- Use 2-space indentation.
- Do not commit tokens, sessions, `.dev.vars`, `.env`, generated logs, caches, or runtime lease state.
- Do not market this as guaranteed pre-edit filesystem locking. Unity often reports dirty state after the first modification.
- Warnings may interrupt saving, but the extension must always provide a safe path that preserves local work.
- Implement production code on short-lived branches even though this plan document was committed directly to `master`.

## Protocol

Use protocol version `1` and a flat JSON envelope that Unity can deserialize without adding a JSON package.

```json
{
  "protocolVersion": 1,
  "type": "lease.acquire",
  "requestId": "f4acb47d-b239-46b4-902f-9f3fd6153cb5",
  "projectId": "potion-panic",
  "connectionId": "b9209e03-5b3c-42ca-8079-0780a3146129",
  "path": "Assets/Scenes/SampleScene.unity",
  "branch": "feature/player-movement",
  "task": "PP-014 Player movement",
  "state": "editing",
  "force": false
}
```

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

- Every message uses `protocolVersion: 1`.
- Every mutating request has a unique `requestId`.
- Paths are repository-relative, use `/`, never start with `/`, and cannot contain `..`.
- The server derives developer identity from the authenticated session.
- One path has at most one `editing` or `reserved` lease.
- Presence may contain multiple developers for one path.
- Duplicate request IDs return the previous result for at least five minutes.
- Reservations last exactly 1,800 seconds.
- Accepted heartbeats extend editing expiry to current server time plus 120 seconds.
- Messages larger than 16 KiB are rejected.

## Configuration contract

Create `coordination.json` at the repository root:

```json
{
  "schemaVersion": 1,
  "projectId": "potion-panic",
  "serverHttpUrl": "https://potion-panic-coordination.<account>.workers.dev",
  "serverWebSocketUrl": "wss://potion-panic-coordination.<account>.workers.dev",
  "heartbeatSeconds": 30,
  "disconnectExpirySeconds": 120,
  "reservationSeconds": 1800,
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

Add important prefab paths only after confirming their actual repository locations. Do not begin with a broad `Assets/**/*.prefab` rule. Add disabled rules for `ProjectSettings/**`, `Packages/manifest.json`, and `Packages/packages-lock.json` for the later shared-configuration milestone.

## File map

### Repository root

- Create `coordination.json`: shared project ID, URLs, timing, and coordinated path rules.
- Modify `.gitignore`: ignore Cloudflare secrets, local coordination settings, logs, and caches.

### Cloudflare service

Create:

```text
Tools/CoordinationServer/package.json
Tools/CoordinationServer/tsconfig.json
Tools/CoordinationServer/wrangler.jsonc
Tools/CoordinationServer/vitest.config.ts
Tools/CoordinationServer/.dev.vars.example
Tools/CoordinationServer/README.md
Tools/CoordinationServer/src/index.ts
Tools/CoordinationServer/src/env.ts
Tools/CoordinationServer/src/protocol.ts
Tools/CoordinationServer/src/auth/crypto.ts
Tools/CoordinationServer/src/auth/session.ts
Tools/CoordinationServer/src/coordination-object.ts
Tools/CoordinationServer/scripts/issue-token.mjs
Tools/CoordinationServer/test/protocol.test.ts
Tools/CoordinationServer/test/session.test.ts
Tools/CoordinationServer/test/leases.test.ts
Tools/CoordinationServer/test/websocket.test.ts
```

### Unity editor client

Create focused files under `Assets/Scripts/Editor/Coordination`:

```text
Model/CoordinationConfig.cs
Model/CoordinationMessage.cs
Model/CoordinationState.cs
Configuration/CoordinationConfigLoader.cs
Configuration/CoordinationUserSettings.cs
Security/ICredentialStore.cs
Security/WindowsCredentialStore.cs
Networking/CoordinationHttpClient.cs
Networking/CoordinationWebSocketClient.cs
Networking/CoordinationReconnectPolicy.cs
Services/CoordinationService.cs
Services/GitContextProvider.cs
Tracking/CoordinatedPathMatcher.cs
Tracking/SceneCoordinationTracker.cs
Tracking/PrefabCoordinationTracker.cs
Tracking/SaveConflictProcessor.cs
UI/CoordinationWindow.cs
UI/CoordinationNotifications.cs
CoordinationBootstrap.cs
```

Create matching EditMode tests under `Assets/Tests/EditMode/Coordination`.

## Task 1: Establish configuration and protocol models

**Files:** `coordination.json`, `src/protocol.ts`, C# model files, path matcher tests.

- [ ] Add failing TypeScript tests for accepted message types, protocol version rejection, normalized paths, `..` rejection, and the 16 KiB limit.
- [ ] Add failing Unity tests for configuration loading, rule matching, slash normalization, and disabled rules.
- [ ] Implement the smallest TypeScript parser and C# DTO/config loader that pass the tests.
- [ ] Verify unknown protocol versions fail explicitly rather than being ignored.
- [ ] Run backend tests and Unity EditMode tests.
- [ ] Commit with `feat(coordination): define configuration and protocol`.

## Task 2: Scaffold the Cloudflare service

**Files:** server package files, Wrangler config, `src/index.ts`, `src/env.ts`.

- [ ] Configure strict TypeScript, Vitest Workers integration, and a SQLite-backed Durable Object binding named `COORDINATION`.
- [ ] Add a `GET /health` endpoint returning protocol version and service status.
- [ ] Add project routing so `projectId` maps to one Durable Object instance.
- [ ] Add tests proving unsupported projects and malformed requests fail with clear status codes.
- [ ] Run `npm test` and `npm run check` from `Tools/CoordinationServer`.
- [ ] Commit with `feat(coordination): scaffold Cloudflare service`.

## Task 3: Implement developer-token and session authentication

**Files:** `src/auth/crypto.ts`, `src/auth/session.ts`, `scripts/issue-token.mjs`, authentication tests.

- [ ] Generate developer tokens from 32 random bytes and display plaintext only once.
- [ ] Store only an HMAC-SHA-256 digest and developer metadata server-side.
- [ ] Add an authenticated administration endpoint protected by a separate Worker secret.
- [ ] Exchange a developer token for a signed 24-hour session.
- [ ] Reject expired, altered, revoked, and wrong-project sessions.
- [ ] Make token revocation prevent new sessions without breaking unrelated developers.
- [ ] Document issuance, rotation, and revocation in the server README.
- [ ] Commit with `feat(coordination): add developer authentication`.

## Task 4: Implement authoritative presence and lease state

**Files:** `src/coordination-object.ts`, lease tests.

- [ ] Create SQLite tables for current developers, current presence, current leases, and recent request IDs.
- [ ] Implement atomic `presence.open` and `presence.close`.
- [ ] Implement `lease.acquire`, returning the existing owner when denied.
- [ ] Implement owner-only release and reservation.
- [ ] Implement immediate override transfer and a displaced-owner event.
- [ ] Implement 30-second heartbeats and 120-second editing expiry.
- [ ] Use Durable Object alarms to remove expired editing leases and reservations.
- [ ] Keep no historical event table.
- [ ] Add concurrency tests proving two simultaneous acquisitions produce one owner.
- [ ] Commit with `feat(coordination): implement lease state machine`.

## Task 5: Add hibernating WebSocket synchronization

**Files:** WebSocket handling in `src/index.ts` and `src/coordination-object.ts`, WebSocket tests.

- [ ] Authenticate before upgrading to WebSocket.
- [ ] Attach project, developer, and connection metadata to each accepted socket.
- [ ] Send a complete snapshot immediately after connection.
- [ ] Broadcast presence and lease changes to all project clients.
- [ ] Restore socket metadata correctly after Durable Object hibernation.
- [ ] Remove connection presence on clean close while allowing leases to expire normally after abrupt disconnect.
- [ ] Verify reconnect and duplicate-request behavior.
- [ ] Commit with `feat(coordination): synchronize leases over WebSockets`.

## Task 6: Store credentials and create Unity session handling

**Files:** credential store, HTTP client, settings, related EditMode tests.

- [ ] Define `ICredentialStore` so secure storage can be mocked.
- [ ] Implement Windows Credential Manager access with `CredWriteW`, `CredReadW`, and `CredDeleteW`.
- [ ] Use credential target `PotionPanic/Coordination/<projectId>/<developerId>`.
- [ ] Never log token or session values.
- [ ] Build a setup UI that asks for developer ID, display name, and token once.
- [ ] Exchange the saved token for a 24-hour session and refresh it automatically before expiry.
- [ ] Add `Forget credentials` and authentication-failure recovery actions.
- [ ] Commit with `feat(coordination): add secure Unity authentication`.

## Task 7: Build the Unity WebSocket service and local state model

**Files:** networking clients, reconnect policy, service, state tests.

- [ ] Connect with the current session and request a snapshot.
- [ ] Marshal all state updates onto the Unity editor main thread.
- [ ] Reconnect with bounded exponential backoff and jitter.
- [ ] Re-authenticate automatically when a session expires.
- [ ] Expose `Connected`, `Reconnecting`, `Offline`, and `AuthenticationFailed` states.
- [ ] Never queue authoritative lease mutations while offline.
- [ ] Republish current presence and reclaim dirty files after reconnect.
- [ ] Commit with `feat(coordination): add Unity connection service`.

## Task 8: Track scene presence and first-dirty claims

**Files:** `SceneCoordinationTracker.cs`, bootstrap, tests.

- [ ] Register `EditorSceneManager.sceneOpened`, `sceneDirtied`, `sceneSaved`, and `sceneClosed` handlers.
- [ ] Publish presence only for paths enabled by `coordination.json`.
- [ ] Request one lease on the first dirty transition, not on every dirty callback.
- [ ] Release the lease on normal close unless a reservation was requested.
- [ ] Restore trackers after domain reload and republish loaded coordinated scenes.
- [ ] Warn immediately when a remotely claimed scene is opened.
- [ ] Test duplicate callbacks, untitled scenes, additive scenes, and domain reload.
- [ ] Commit with `feat(coordination): coordinate Unity scenes`.

## Task 9: Track selected important prefabs

**Files:** `PrefabCoordinationTracker.cs`, configuration update, tests.

- [ ] Use Prefab Stage open, dirty, save, and closing events.
- [ ] Resolve the prefab asset path and apply the same presence and lease rules as scenes.
- [ ] Keep prefab coordination allowlisted by exact path in the first release.
- [ ] Do not claim nested dependencies or every referenced prefab automatically.
- [ ] Test opening, modifying, closing, domain reload, and a non-coordinated prefab.
- [ ] Commit with `feat(coordination): coordinate selected prefabs`.

## Task 10: Guard saves and implement deliberate override

**Files:** `SaveConflictProcessor.cs`, service override API, tests.

- [ ] Intercept scene and asset save callbacks where Unity permits it.
- [ ] When another developer owns the lease, show owner, branch, task, and expiry.
- [ ] Provide `Cancel Save` and `Override Claim and Save` while online.
- [ ] Provide `Save Without Claim` only when offline.
- [ ] Transfer ownership before continuing an online override save.
- [ ] If override communication fails, preserve local changes and require another explicit decision.
- [ ] Prevent recursive save prompts after a confirmed override.
- [ ] Commit with `feat(coordination): guard conflicting saves`.

## Task 11: Build the Coordination window and Unity notifications

**Files:** UI files and UI state tests.

- [ ] Add `Window > Potion Panic > Coordination`.
- [ ] Show connection state, authenticated developer, current branch, task context, open presence, active leases, and reservations.
- [ ] Add actions for reconnect, reserve 30 minutes, release own lease, override, copy path, and forget credentials.
- [ ] Show prominent offline and authentication-failure banners.
- [ ] Notify only for important events: remote claim, conflict, override, reservation, authentication failure, and prolonged disconnect.
- [ ] Do not send disruptive notifications for ordinary viewing presence or clean release.
- [ ] Commit with `feat(coordination): add Unity coordination interface`.

## Task 12: Add offline reconciliation and lifecycle hardening

**Files:** service, bootstrap, trackers, reconnection tests.

- [ ] Release presence and owned leases on normal Unity shutdown where possible.
- [ ] Treat abrupt shutdown as a normal stale-lease expiry case.
- [ ] After reconnect, publish current scenes and prefab stages, then request leases for dirty coordinated files.
- [ ] Surface conflicts using the normal override workflow.
- [ ] Ensure script compilation and domain reload recreate services without duplicate event subscriptions.
- [ ] Add a local `Disabled` switch that stops networking and tracking without affecting project editability.
- [ ] Commit with `fix(coordination): harden reconnect and editor lifecycle`.

## Task 13: Deploy and perform two-machine acceptance testing

- [ ] Create Cloudflare secrets for session signing and administration.
- [ ] Deploy the Worker and Durable Object.
- [ ] Replace placeholder URLs in `coordination.json`.
- [ ] Issue one token per developer and configure both machines.
- [ ] Verify both developers connect from different networks.
- [ ] Verify opening publishes presence without acquiring a lease.
- [ ] Verify first modification acquires a lease and notifies the other client.
- [ ] Verify a second save warns and an override transfers ownership.
- [ ] Verify a clean close releases immediately.
- [ ] Verify a crashed client lease expires within 120 seconds.
- [ ] Verify a reservation lasts 30 minutes.
- [ ] Verify offline editing remains possible with a prominent warning.
- [ ] Verify token revocation blocks new sessions.
- [ ] Verify the shared scene still enters Play Mode without new Console errors.

## Task 14: Documentation and first-release handoff

**Files:** onboarding guide, team workflow, server README, this plan, and related board ticket.

- [ ] Replace manual coordination instructions with the automated workflow while retaining a manual fallback for outages.
- [ ] Document first-time token setup and the expected once-per-machine lifecycle.
- [ ] Document claim, reservation, override, offline, and recovery behavior.
- [ ] State explicitly that the tool provides coordination warnings rather than hard filesystem locks.
- [ ] Run `npm run docs:build`.
- [ ] Record backend tests, Unity EditMode tests, Play Mode smoke test, and two-machine acceptance evidence.
- [ ] Move this plan to the archive only after the first usable release is deployed and accepted.

## Later milestone: shared project configuration

After scenes and prefabs are stable, enable coordination for:

```text
ProjectSettings/**
Packages/manifest.json
Packages/packages-lock.json
```

Use debounced filesystem and asset-modification detection. Compare metadata before requesting claims so Unity imports and package resolution do not create false claims. Apply the same save warning and override behavior as scenes and prefabs.

## Later milestone: Windows notifications

Add native Windows notifications only after Unity notifications prove reliable. Notify when Unity is minimized or unfocused for remote claims, conflicts, overrides, reservations, authentication failure, and prolonged disconnection. Do not make Windows notification support a dependency of coordination correctness.

## Optional Docboard integration

Add a read-only active-claims panel only after the Unity workflow is complete. The Cloudflare service remains authoritative. Do not place developer tokens in generated static documentation and do not let Docboard mutate leases in the first integration.

## Verification commands

Backend:

```powershell
cd Tools/CoordinationServer
npm test
npm run check
```

Documentation:

```powershell
npm run docs:build
```

Unity:

1. Open Unity `6000.5.1f1`.
2. Run the complete coordination EditMode test assembly.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Enter Play Mode and confirm no new relevant Console errors.
5. Exit Play Mode and verify coordination reconnects or remains connected.

Repository review:

```powershell
git status
git diff --check
git diff master...HEAD --stat
```

Confirm that no secret files, tokens, sessions, generated Unity folders, logs, or unrelated scene changes are present.

## Acceptance criteria

The first usable release is complete only when:

- Both developers connect from different networks.
- Opening a coordinated scene or selected prefab shows viewing presence.
- First modification acquires one authoritative lease.
- Simultaneous acquisition produces exactly one owner.
- Remote owner, branch, and task context are visible.
- Saving against a remote owner requires a deliberate decision.
- Override transfers ownership and notifies both developers.
- Clean close releases immediately.
- Abrupt-disconnect leases expire within 120 seconds.
- Reservations expire after 30 minutes.
- Offline mode never prevents preserving local work.
- Credentials survive Unity restarts without re-entry.
- Revocation prevents new sessions for only the revoked developer.
- No activity history is retained.
- Backend and Unity tests pass.
- `SampleScene.unity` still enters Play Mode cleanly.
- The documentation build passes.

## Rollback

If the Unity client destabilizes the editor, enable the local `Disabled` switch, stop trackers and networking, preserve project editability, and revert the Unity client commit range if required. If the backend fails, clients enter offline mode and the team temporarily uses explicit manual announcements. Do not rotate developer tokens unless the incident involves credential exposure.
