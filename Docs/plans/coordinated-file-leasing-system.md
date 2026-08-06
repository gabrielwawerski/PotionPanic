---
title: Coordinated File Leasing Program
---

# Coordinated File Leasing Program

> **For agentic workers:** Execute exactly one linked slice per Codex session.
> Read this program page and the selected slice before editing. Do not combine
> slices in one session. Each slice must finish with its own verification and a
> focused commit before the next slice starts.

**Goal:** Add advisory coordination for Unity scenes and selected prefabs so
developers can see file presence, claim editing leases, reserve files, and make
conflicting saves deliberate while authoritative state lives in a Cloudflare
Durable Object.

**Architecture:** The Unity editor extension and Cloudflare Worker stay in this
repository for the first release. One SQLite-backed Durable Object per project
owns developers, sessions, connections, presence, leases, reservations, replay
records, and the monotonic state version. The Unity client stores a developer
token in Windows Credential Manager, keeps the opaque session in memory, and
resumes a blocked save only after an authoritative result.

**Tech stack:** Unity `6000.5.1f1`, C# editor scripting, Unity Test Framework
EditMode tests, Windows Credential Manager, TypeScript, Cloudflare Workers,
SQLite-backed Durable Objects, WebSocket Hibernation API, Wrangler, and Vitest.

## Execution contract

- One session owns one slice page.
- Start by checking `git status --short`, reading the slice, and confirming its
  dependency commits are present.
- Stay within the slice's file map. Do not edit scenes, prefabs, packages, or
  project settings for this program.
- Use TDD for behavior: add the focused failing test, implement the smallest
  production change, run the focused test, then run the slice gate.
- Commit only the slice's files with the commit message specified on its page.
- Append the commit hash, commands, results, and unresolved risk to `PP-7`
  before ending the session. Do not mark the ticket complete until Slice 09.

## Global approved behavior

- Opening a coordinated file publishes non-exclusive `viewing` presence.
- The first meaningful modification requests an exclusive `editing` lease.
- A developer may reserve an unclaimed coordinated file before editing it.
- Initial coordinated files are all scenes and an explicit allowlist of
  important prefabs.
- Healthy clients heartbeat every 30 seconds.
- Disconnected viewing presence and editing leases expire after 120 seconds.
- Reservations last 30 minutes and survive the owning connection closing.
- Closing an editing file releases its lease unless the developer reserved it.
- A remote claim shows owner, branch, task, and expiry. Saving requires cancel or
  explicit override.
- Override transfers ownership immediately and notifies the displaced developer.
- Backend outages never block local editing or permanently trap unsaved work.
- Notifications and the Coordination window are the first UI. Native Windows
  notifications and Rider integration are out of scope.
- The backend stores current coordination state and short-lived replay records,
  never activity history.
- Authentication uses one revocable developer token per developer, exchanged
  for an opaque 24-hour session.
- Machine-local task context, preferences, and endpoint overrides stay outside
  Git. Repository rules live in tracked `coordination.json`.
- This is advisory coordination, not a hard filesystem lock. Manual protected
  change announcements remain required during outages.

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
- Use 2-space indentation. Never commit tokens, sessions, `.dev.vars`, local
  settings, logs, caches, or generated runtime lease state.
- Windows is the only supported editor host for the first release. Unsupported
  hosts show `Disabled`.
- Derive HTTP and WebSocket URLs from the one committed `serverBaseUrl`.
- `**/` matches zero or more directories, so the direct
  `Assets/Scenes/SampleScene.unity` path matches the initial rule.
- Add prefab rules only after confirming actual paths; begin with exact
  allowlisted paths rather than `Assets/**/*.prefab`.

## Protocol v1 contract

Use flat JSON envelopes that Unity can deserialize without another JSON package.
The endpoint and authenticated session, never client payload fields, determine
the project, developer, and connection.

```text
POST /v1/projects/{projectId}/sessions
Authorization: Bearer <developer-token>

GET /v1/projects/{projectId}/connect
Authorization: Bearer <session-token>
Upgrade: websocket
```

The session response contains `developerId`, `displayName`, `serverTime`,
`connectionId`, `leaseTtlSeconds`, `reservationTtlSeconds`, and `stateVersion`.

Every client-to-server envelope contains these fields:

```json
{
  "protocolVersion": 1,
  "type": "lease.acquire",
  "requestId": "a UUID v4 string"
}
```

`requestId` is mandatory for every client message, including `snapshot.request`.
The server records replay results only for mutating messages. Client envelopes
must not contain `projectId`, `developerId`, or `connectionId`.

| Client message | Required additional fields | Meaning |
| --- | --- | --- |
| `presence.open` | `path`, `branch`, `task` | Publish non-exclusive viewing presence. |
| `presence.close` | `path` | Remove this connection's viewing presence. |
| `lease.acquire` | `path`, `branch`, `task` | Claim an unclaimed path for editing. |
| `lease.release` | `path` | Release this developer's editing lease. |
| `lease.reserve` | `path`, `branch`, `task` | Reserve an unclaimed path. |
| `lease.override` | `path`, `branch`, `task` | Transfer a remotely owned lease deliberately. |
| `heartbeat` | none | Extend only this connection's presence and editing leases. |
| `snapshot.request` | none | Request the complete current state; no history replay exists. |

`path` is the submitted display path. The server normalizes separators and
Unicode, rejects control characters, leading separators, drive prefixes, `.`
and `..` segments, and derives a lower-invariant canonical key. It preserves the
normalized submitted casing as `displayPath`. A path is at most 1,024 UTF-16
code units; `branch` and `task` are each at most 256 UTF-16 code units. The
serialized UTF-8 envelope is at most 16 KiB.

Every server-to-client envelope contains `protocolVersion: 1`, `type`, and the
current monotonic `stateVersion`. A response to a request also contains that
request's `requestId`. The client applies a state-carrying envelope only when
its `stateVersion` is not older than the greatest version already applied.

| Server message | Required additional fields |
| --- | --- |
| `session.ready` | `developerId`, `displayName`, `serverTime`, `connectionId`, `leaseTtlSeconds`, `reservationTtlSeconds` |
| `snapshot` | `presence`, `leases`, `serverTime` |
| `presence.updated` | `presence` |
| `presence.removed` | `path`, `connectionId` |
| `lease.granted` | `path`, `lease` |
| `lease.denied` | `path`, `code`, `currentLease` |
| `lease.updated` | `lease` |
| `lease.released` | `path`, `leaseId` |
| `lease.overridden` | `path`, `previousDeveloperId`, `lease` |
| `error` | `code`, `message` |

`presence` is an array of presence records; each record contains `path`,
`displayPath`, `developerId`, `displayName`, `connectionId`, `branch`, `task`,
and `expiresAt`. `leases` is an array of lease records; each record contains
`leaseId`, `path`, `displayPath`, `mode` (`editing` or `reserved`),
`developerId`, `displayName`, `branch`, `task`, `expiresAt`, and `connectionId`
only for editing leases. A `lease.denied` response uses `currentLease: null`
when the path became unavailable without a lease record.

One canonical path has at most one editing or reserved lease, while presence may
contain multiple connections. Replay records are scoped to developer and request
ID, contain a payload hash, return the earlier result only for an identical
payload for five minutes, and reject mismatched reuse. The server accepts a
heartbeat from its authenticated owning connection even if unrelated state has
advanced; stale server state is rejected by the client at apply time.

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
06 Scene and prefab tracking
             ↓
07 Conflict-safe save guard
             ↓
08 Coordination UI and lifecycle
             ↓
09 Deployment, acceptance, documentation, handoff
```

Slice 01 includes the repository baseline and protocol contract. Slice 02
creates identities and authenticated sessions. Slice 03 owns all state
transitions and expiry. Slice 04 transports that state. Slices 05 through 08
consume the stable backend contract in order. Slice 09 is the only release gate.

## Session slices

1. [Foundations, configuration, and protocol](./coordinated-file-leasing-01-foundations.md)
2. [Developer and session authentication](./coordinated-file-leasing-02-authentication.md)
3. [Authoritative state and expiry](./coordinated-file-leasing-03-authoritative-state.md)
4. [Hibernating WebSocket synchronization](./coordinated-file-leasing-04-websocket-sync.md)
5. [Unity authentication and connection service](./coordinated-file-leasing-05-unity-connection.md)
6. [Scene and selected-prefab tracking](./coordinated-file-leasing-06-asset-tracking.md)
7. [Conflict-safe save guard](./coordinated-file-leasing-07-save-guard.md)
8. [Coordination window and lifecycle](./coordinated-file-leasing-08-ui-lifecycle.md)
9. [Release acceptance and documentation handoff](./coordinated-file-leasing-09-release-handoff.md)

## Release acceptance

The program is complete only after two developers connect from different
networks using opaque sessions; presence, reservations, leases, reconnect,
hibernation, override, clean close, abrupt termination, network loss, outage,
revocation, and 120-second stale expiry are demonstrated; local work survives
pending, failed, offline, and domain-reload save paths; no secret or generated
state enters Git; backend tests, Unity EditMode tests, Play Mode smoke testing,
the documentation build, and two-machine evidence are recorded in `PP-7`.

The root documentation suite may be called clean because PP-8 restored its
baseline before this program began. Keep that fact in the ticket notes rather
than adding PP-8 work to a coordination slice.

## Rollback

If the Unity client destabilizes the editor, use the local `Disabled` switch to
stop networking and tracking while preserving local editability, then revert
coordination client commits if required. If the backend fails, use offline mode
and explicit manual announcements. Rotate developer tokens only for credential
exposure.
