---
title: 'Coordinated Leasing 05: Unity Authentication and Connection Service'
---

# Coordinated Leasing 05: Unity Authentication and Connection Service

**Session goal:** Connect the Unity editor securely to the stable Worker while
keeping developer credentials durable only in Windows Credential Manager and
sessions in memory.

**Depends on:** Slices 01 and 04.

**Produces:** A main-thread-safe editor service exposing connection state and
authenticated protocol operations to later Unity slices.

## Files

- Create focused files under `Assets/Scripts/Editor/Coordination/` for
  `ICredentialStore`, Windows credential access, session models, HTTP client,
  WebSocket client, reconnect policy, `CoordinationService`, and Git context.
- Create `CoordinationCredentialWindow.cs` as the narrow token-entry surface
  required before the full Coordination window exists in Slice 08.
- Add matching tests under `Assets/Tests/EditMode/Coordination/`.
- Modify only the existing Editor assembly references if compilation requires
  it.

## Implementation steps

- Define `ICredentialStore` and a mock implementation for tests. On Windows,
  guard Credential Manager P/Invoke with editor compilation conditions and store
  only `PotionPanic/Coordination/<projectId>/developer-token`.
- Prompt once for a developer token, exchange it for a session, and keep the
  session token in memory. Use `CoordinationCredentialWindow` for token entry;
  it passes the token directly to `ICredentialStore` and must never serialize it
  into the local settings file. Provide explicit forget-credentials behavior.
- Implement HTTP session creation and the authenticated WebSocket upgrade. Map
  service states to `Connected`, `Reconnecting`, `Offline`,
  `AuthenticationFailed`, and `Disabled`.
- Marshal socket callbacks to Unity's main thread, restore snapshots after
  reconnect, and refresh expired sessions. Never queue lease mutations while
  offline.
- Read the endpoint override and local `Disabled` switch from Slice 01's local
  settings store. Derive the branch from Git and read the task context from that
  store when sending `presence.open`, `lease.acquire`, `lease.reserve`, or
  `lease.override`.
- Expose typed events or callbacks for `session.ready`, snapshots, presence,
  lease results, errors, and revocation without coupling the service to scene
  tracking or UI.

## Verification

Unity Test Runner, Coordination EditMode tests:

- mock credential setup, read, and forget;
- server identity and TTL handling;
- session refresh and invalid-token failure;
- no session persistence;
- unsupported-platform `Disabled` state;
- credential-window submission and token-free local settings;
- main-thread dispatch and reconnect state transitions;
- no queued mutations while offline.

Run the full Coordination EditMode suite after the focused tests and confirm no
new Unity Console errors during editor compilation and domain reload.

**Commit:** `feat(coordination): add secure Unity connection service`

**Handoff:** Record the public service surface, commit, and test output in
`PP-7`. Slice 06 may subscribe to this service but must not alter its auth or
transport behavior.
