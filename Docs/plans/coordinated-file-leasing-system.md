---
title: Coordinated File Leasing Program
---

# Coordinated File Leasing Program

> **For agentic workers:** Execute exactly one linked slice per Codex session.
> Read this program page and the selected slice before editing. Do not combine
> slices in one session. Each slice must finish with its own verification and a
> focused commit before the next slice starts.

**Goal:** Add advisory coordination for Unity scenes and selected prefabs so developers can see file presence, claim editing leases, reserve files, and make conflicting saves deliberate while authoritative state lives in a Cloudflare Durable Object.

**Architecture:** The Unity editor extension and Cloudflare Worker stay in this repository for the first release. One SQLite-backed Durable Object per project owns developers, sessions, connections, presence, leases, reservations, replay records, and the monotonic state version. The Unity client stores a developer token in Windows Credential Manager, keeps the opaque session in memory, and resumes a blocked save only after an authoritative result.

**Tech stack:** Unity `6000.5.1f1`, C# editor scripting, Unity Test Framework EditMode tests, Windows Credential Manager, TypeScript, Cloudflare Workers, SQLite-backed Durable Objects, WebSocket Hibernation API, Wrangler, and Vitest.

## Execution contract

- One session owns one slice page.
- Start by checking `git status --short`, reading the slice, and confirming its dependency commits are merged into `master`. Create a fresh branch named
  `feature/coordination-<slice-number>-<short-name>` from that `master` commit.
- Stay within the slice's file map. Do not edit scenes, prefabs, packages, or project settings for this program.
- Use TDD for behavior: add the focused failing test, implement the smallest production change, run the focused test, then run the slice gate.
- Commit only the slice's files with the commit message specified on its page.
- Append the commit hash, commands, results, and unresolved risk to `PP-7`
  before ending the session. Do not mark the ticket complete until Slice 09.

## Global approved behavior

- Opening a coordinated file publishes non-exclusive `viewing` presence.
- The first meaningful modification requests an exclusive `editing` lease.
- A developer may reserve an unclaimed coordinated file before editing it.
- Initial coordinated files are all scenes and an explicit allowlist of important prefabs.
- Healthy clients heartbeat every 30 seconds.
- Disconnected viewing presence and editing leases expire after 120 seconds.
- Reservations last 30 minutes and survive the owning connection closing.
- A developer may explicitly cancel its reservation from any authenticated connection for that developer.
- Closing an editing file releases its lease unless the developer reserved it.
- A remote claim shows owner, branch, task, and expiry. Saving requires cancel or explicit override.
- Override transfers ownership immediately and notifies the displaced developer.
- Backend outages never block local editing or permanently trap unsaved work.
- Notifications and the Coordination window are the first UI. Native Windows notifications and Rider integration are out of scope.
- The backend stores current coordination state and short-lived replay records, never activity history.
- Authentication uses one revocable developer token per developer, exchanged for an opaque 24-hour session.
- Machine-local task context, preferences, and endpoint overrides stay outside Git. Repository rules live in tracked `coordination.json`.
- This is advisory coordination, not a hard filesystem lock. Manual protected change announcements remain required during outages.

## Repository boundary and non-negotiable constraints

```text
PotionPanic/
├── coordination.json
├── Assets/Scripts/Editor/Coordination/
├── Assets/Tests/EditMode/Coordination/
├── Tools/CoordinationServer/
└── Docs/
```

- Reuse `PotionPanic.Editor`; add it to the existing EditMode test assembly.
- Keep runtime gameplay assemblies independent of coordination code.
- Put the backend under `Tools/CoordinationServer` and commit its lockfile.
- Use 2-space indentation. Never commit tokens, sessions, `.dev.vars`, local settings, logs, caches, or generated runtime lease state.
- Windows is the only supported editor host for the first release. Unsupported hosts show `Disabled`.
- Derive HTTP and WebSocket URLs from the one committed `serverBaseUrl`.
- `**/` matches zero or more directories, so the direct
  `Assets/Scenes/SampleScene.unity` path matches the initial rule.
- Add prefab rules only after confirming actual paths; begin with exact allowlisted paths rather than `Assets/**/*.prefab`.
- Store local endpoint overrides, the manual task context, and the local
  `Disabled` switch only in
  `UserSettings/PotionPanic/coordination.local.json`. The file is untracked and must contain no developer or session token. Its v1 shape is:

  ```json
  {
    "schemaVersion": 1,
    "serverBaseUrlOverride": "",
    "taskContext": "",
    "disabled": false
  }
  ```

  The client derives `branch` from Git at send time and reads `task` from this local settings file. An unavailable Git branch is sent as an empty string.

## Protocol v1 contract

Use flat JSON envelopes that Unity can deserialize without another JSON package. The endpoint and authenticated session, never client payload fields, determine the project, developer, and connection.

```text
GET /health

POST /v1/projects/{projectId}/sessions
Authorization: Bearer <developer-token>

GET /v1/projects/{projectId}/connect
Authorization: Bearer <session-token>
Upgrade: websocket

POST /v1/projects/{projectId}/developers
Authorization: Bearer <ADMIN_TOKEN>

DELETE /v1/projects/{projectId}/developers/{developerId}
Authorization: Bearer <ADMIN_TOKEN>
```

`GET /health` returns HTTP 200 with `service: "potion-panic-coordination"` and
`serverTime`. It accepts no credential and exposes no project or developer data.

The session response contains `developerId`, `displayName`, `serverTime`,
`leaseTtlSeconds`, `reservationTtlSeconds`, and `stateVersion`. It never contains
`connectionId`. A successful WebSocket upgrade creates the connection and
`session.ready` returns its server-assigned `connectionId`.

The administrator create-developer request contains only `displayName`; its response contains `developerId`, `displayName`, and one `developerToken` value. The caller displays that token once and never logs or persists it. The delete route marks the developer revoked, deletes its sessions, and returns HTTP 204. The WebSocket slice closes that developer's active sockets immediately.

Every client-to-server envelope contains these fields:

```json
{
  "protocolVersion": 1,
  "type": "lease.acquire",
  "requestId": "a UUID v4 string"
}
```

`requestId` is mandatory for every client message, including `snapshot.request`. The server records replay results only for mutating messages. Client envelopes must not contain `projectId`, `developerId`, or `connectionId`.

| Client message       | Required additional fields | Meaning                                                       |
|----------------------|----------------------------|---------------------------------------------------------------|
| `presence.open`      | `path`, `branch`, `task`   | Publish non-exclusive viewing presence.                       |
| `presence.close`     | `path`                     | Remove this connection's viewing presence.                    |
| `lease.acquire`      | `path`, `branch`, `task`   | Claim an unclaimed path for editing.                          |
| `lease.release`      | `path`                     | Release this developer's editing lease.                       |
| `lease.reserve`      | `path`, `branch`, `task`   | Reserve an unclaimed path.                                    |
| `reservation.cancel` | `path`                     | Cancel this developer's reservation.                          |
| `lease.override`     | `path`, `branch`, `task`   | Transfer a remotely owned lease deliberately.                 |
| `heartbeat`          | none                       | Extend only this connection's presence and editing leases.    |
| `snapshot.request`   | none                       | Request the complete current state; no history replay exists. |

`path` is the submitted display path. The server normalizes separators and Unicode, rejects control characters, leading separators, drive prefixes, `.`
and `..` segments, and derives a lower-invariant canonical key. It preserves the normalized submitted casing as `displayPath`. A path is at most 1,024 UTF-16 code units; `branch` and `task` are each at most 256 UTF-16 code units. The serialized UTF-8 envelope is at most 16 KiB.

`lease.release` is connection-owned and applies only to an editing lease.
`reservation.cancel` is developer-owned, so a recreated session or another connection for the same developer may cancel the reservation. Successful cancellation uses the existing correlated `lease.released` server envelope; the reservation ID is its `leaseId`.

Every server-to-client envelope contains `protocolVersion: 1`, `type`, and the current monotonic `stateVersion`. A response to a request also contains that request's `requestId`. The client applies a state-carrying envelope only when its `stateVersion` is not older than the greatest version already applied.

| Server message     | Required additional fields                                                                             |
|--------------------|--------------------------------------------------------------------------------------------------------|
| `session.ready`    | `developerId`, `displayName`, `serverTime`, `connectionId`, `leaseTtlSeconds`, `reservationTtlSeconds` |
| `snapshot`         | `presence`, `leases`, `serverTime`                                                                     |
| `presence.updated` | `presence`                                                                                             |
| `presence.removed` | `path`, `connectionId`                                                                                 |
| `lease.granted`    | `path`, `lease`                                                                                        |
| `lease.denied`     | `path`, `code`, `currentLease`                                                                         |
| `lease.updated`    | `lease`                                                                                                |
| `lease.released`   | `path`, `leaseId`                                                                                      |
| `lease.overridden` | `path`, `previousDeveloperId`, `lease`                                                                 |
| `error`            | `code`, `message`                                                                                      |

`presence` is an array of presence records; each record contains `path`,
`displayPath`, `developerId`, `displayName`, `connectionId`, `branch`, `task`, and `expiresAt`. `leases` is an array of lease records; each record contains
`leaseId`, `path`, `displayPath`, `mode` (`editing` or `reserved`),
`developerId`, `displayName`, `branch`, `task`, `expiresAt`, and `connectionId`
only for editing leases. A `lease.denied` response uses `currentLease: null`
when the path became unavailable without a lease record.

One canonical path has at most one editing or reserved lease, while presence may contain multiple connections. Replay records are scoped to developer and request ID, contain a payload hash, return the earlier result only for an identical payload for five minutes, and reject mismatched reuse. The server accepts a heartbeat from its authenticated owning connection even if unrelated state has advanced; stale server state is rejected by the client at apply time. The authoritative-state slice returns state-transition data only; the WebSocket slice is solely responsible for sending it to clients.

The Durable Object owns all persistent state. Slice 02 creates its auth-only foundation: `developers`, `sessions`, and the initial state-version row plus the object class. Slice 03 extends that same class with `connections`, `presence`,
`leases`, `reservations`, and `replayRecords`. A WebSocket upgrade in Slice 04 creates a connection record; an HTTP session never does.

## Dependency graph

```text
01 Foundations
      ↓
02 Authentication ─────┐
      ↓                │
03 Authoritative state │
      └──────┬─────────┘
             ↓
04 WebSocket synchronization
             ↓
05 Unity connection service
             ↓
05A Authoritative backend stabilization
             ↓
05B Unity connection stabilization
             ↓
06 Scene and prefab tracking
             ↓
07 Conflict-safe save guard
             ↓
08 Coordination UI and lifecycle
             ↓
09 Deployment, acceptance, documentation, handoff
```

Slice 01 includes the repository baseline and protocol contract. Slice 02 creates identities and authenticated sessions. Slice 03 owns all state transitions and expiry. Slice 04 transports that state. Slice 05A stabilizes the authoritative backend and Slice 05B stabilizes the Unity protocol and transport. Slice 06 remains paused until both stabilization gates are green. Slices 06 through 08 then consume the stable contract in order. Slice 09 is the only release gate.

## Session slices

1. [Foundations, configuration, and protocol](./archive/coordinated-file-leasing-01-foundations.md)
2. [Developer and session authentication](./archive/coordinated-file-leasing-02-authentication.md)
3. [Authoritative state and expiry](./archive/coordinated-file-leasing-03-authoritative-state.md)
4. [Hibernating WebSocket synchronization](./archive/coordinated-file-leasing-04-websocket-sync.md)
5. [Unity authentication and connection service](./archive/coordinated-file-leasing-05-unity-connection.md)
6. [Authoritative backend stabilization](./archive/coordinated-file-leasing-stabilization.md#slice-05a-backend-stabilization)
7. [Unity connection stabilization](./archive/coordinated-file-leasing-stabilization.md#slice-05b-unity-client-stabilization)
8. [Scene and selected-prefab tracking](./archive/coordinated-file-leasing-06-asset-tracking.md)
9. [Conflict-safe save guard](./archive/coordinated-file-leasing-07-save-guard.md)
10. [Coordination window and lifecycle](./archive/coordinated-file-leasing-08-ui-lifecycle.md)
11. [Release acceptance and documentation handoff](./coordinated-file-leasing-release-acceptance.md)

## Release acceptance

The program is complete only after two developers connect from different networks using opaque sessions; presence, reservations, leases, reconnect, hibernation, override, clean close, abrupt termination, network loss, outage, revocation, and 120-second stale expiry are demonstrated; local work survives pending, failed, offline, and domain-reload save paths; no secret or generated state enters Git; backend tests, Unity EditMode tests, Play Mode smoke testing, the documentation build, and two-machine evidence are recorded in `PP-7`.

The root documentation suite may be called clean because PP-8 restored its baseline before this program began. Keep that fact in the ticket notes rather than adding PP-8 work to a coordination slice.

## Rollback

If the Unity client destabilizes the editor, use the local `Disabled` switch to stop networking and tracking while preserving local editability, then revert coordination client commits if required. If the backend fails, use offline mode and explicit manual announcements. Rotate developer tokens only for credential exposure.
