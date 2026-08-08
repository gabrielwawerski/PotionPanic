---
id: 7
title: Implement coordinated Unity file leasing
status: todo
priority: high
documentation:
  - plans/coordinated-file-leasing-system.md
  - onboarding/getting-started.md
  - collaboration/team-workflow.md
  - guides/unity/editor-safety.md
affectedFiles:
  - coordination.json
  - Assets/Scripts/Editor/Coordination
  - Assets/Tests/EditMode/Coordination
  - Assets/Tests/EditMode/PotionPanic.EditModeTests.asmdef
  - Tools/CoordinationServer
  - .github/workflows/coordination-server.yml
tags: []
order: 1
assignee: Codex
---

## Description

Implement the tracked coordination program for advisory Unity scene and
selected-prefab presence, leases, reservations, conflict-safe saving, and a
Cloudflare Durable Object backend. Execute one linked plan slice per Codex
session, in dependency order.

## Acceptance Criteria

- [ ] The accepted behavior and verification criteria in
  [`../plans/coordinated-file-leasing-system.md`](../plans/coordinated-file-leasing-system.md)
  are met.
- [ ] Two Windows Unity editors can coordinate from different networks without
  exposing developer or session tokens.
- [ ] Offline mode preserves local work and manual collaboration remains the
  documented fallback.

## Implementation Plan

Follow the nine linked implementation slices in dependency order. Each session
must record its commit hash, verification output, and handoff result in
Implementation Notes. Do not combine slices or mark the ticket complete before
the release-acceptance slice records two-machine evidence.

## Implementation Notes

2026-08-06: Plan split into nine independent Codex session slices. PP-8
restored the root documentation test baseline; `npm test` and
`npm run docs:build` pass. No coordination server or Unity client
implementation has started.

2026-08-06: Slice 01 foundations committed as
`d6563ed868b6b359024ba1ec179c683f5a452313`
(`feat(coordination): scaffold backend and configuration`). Commands passed:
`Tools/CoordinationServer`: `npm ci`, `npm run typecheck`, `npm test` (49),
and `npx wrangler deploy --dry-run`; repository root: `npm test` (11) and
`npm run docs:build`. The Unity Coordination EditMode command could not run:
Unity 6000.5.1f1 exited with code 198 before compilation because no valid
Editor license is activated. `npm ci` also reported four Worker dependency
audit findings (three moderate, one high); no dependency upgrade was made in
this slice.

2026-08-06: Slice 02 authentication committed as
`cb539c2` (`feat(coordination): add revocable opaque sessions`). Commands
passed from `Tools/CoordinationServer`: `npm run typecheck`,
`npm test -- tests/auth` (6), `npm test` (55), and
`npx wrangler deploy --dry-run`. The slice adds domain-separated HMAC-SHA-256
digest-only developer and session persistence, independent administrator
authentication, revocation, the one-time issuance script, and no-connection-ID
HTTP sessions. Risk/handoff: expired session rows are rejected by Slice 02
authentication but remain persisted until Slice 03 implements the program's
authoritative expiry and pruning behavior. Slice 04 remains responsible for
closing a revoked developer's live sockets.

2026-08-06: Slice 03 authoritative state committed as
`74661d4` (`feat(coordination): implement authoritative lease state`). Commands
passed from `Tools/CoordinationServer`: `npm run typecheck`,
`npm test -- tests/state` (9), and `npm test` (64). The Durable Object now owns
connections, presence, editing leases, persistent reservations, replay records,
expiry pruning, and nearest-expiry alarms. A reservation remains persisted while
its owner edits the same path, then becomes the effective reservation again on
lease release or connection close; at most one effective editing or reserved
lease exists for a canonical lower-invariant path. Transitions return requester
and state-change envelopes only. They do not accept a client state version or
hold, send, or broadcast through a WebSocket. Slice 04 must authenticate the
upgrade, call `openConnection`, deliver returned envelopes, and extend the
alarm handler with live-socket broadcasting without duplicating lease logic.

2026-08-06: Slice 04 WebSocket synchronization committed as `a8dc482`
(`feat(coordination): synchronize authoritative state`). Commands passed
from `Tools/CoordinationServer`: `npm run typecheck`,
`npm test -- tests/websocket` (11), `npm test` (75), and
`npx wrangler deploy --dry-run`. The authenticated
`GET /v1/projects/{projectId}/connect` upgrade rejects query strings, creates a
connection only through `openConnection`, and sends `session.ready` followed by
the current snapshot. The Durable Object keeps only server-derived project,
developer, session, and connection metadata in hibernation attachments; it
restores that metadata on wake, routes validated client envelopes without a
client state version, broadcasts authoritative transitions, releases
connection-scoped state on close or revocation, and broadcasts expiry
transitions from its existing alarm. Transport tests cover upgrade
authentication, ordering, server-assigned IDs, mutation broadcasts, hibernation
restore, replay, client version rejection, oversized envelopes, revocation,
alarm expiry broadcasts, and older server-version rejection. Remaining risk:
deployment and two-machine Unity client acceptance remain deferred to later
slices; no live Worker was deployed by this slice.

2026-08-07: Slice 05 Unity connection service committed as `7a9a4f8`
(`feat(coordination): add secure Unity connection service`). The editor service
keeps developer tokens only in Windows Credential Manager at
`PotionPanic/Coordination/<projectId>/developer-token`; opaque sessions remain
in memory. It exposes connection states, authenticated session and WebSocket
transport, one-time credential prompting, reconnect/session refresh, Git branch
and local task context, main-thread event dispatch, and explicit offline mutation
rejection. `CoordinationCredentialWindow` is the limited token-entry and forget
surface before Slice 08. Unity EditMode focused service tests passed 9/9;
the full Coordination suite compiled and ran 40 tests with 39 passing. The
remaining failure predates Slice 05: `CoordinationProtocolTests.
AcceptsEveryV1ServerMessageWithItsRequiredFields` rejects the valid
`lease.denied` envelope with `currentLease: null` in the Slice 01 parser.
No scene, prefab, package, or project-setting files changed. Handoff: Slice 06
may subscribe to `CoordinationService` events and its `Try*` protocol methods,
without changing authentication or transport behavior.

2026-08-08: Stabilization baseline recorded before implementation. The backend
typecheck passed. `npm test -- tests/auth` passed 6/6 and
`npm test -- tests/websocket` passed 11/11, but `npm test -- tests/state`
failed 8/9 and the full `npm test` failed 8/75. The failed state tests use
fixed 2026-08-07 timestamps, which had already expired on this run; the first
failure was `Target cannot be null or undefined` at
`authoritative-state.test.ts:74` because the snapshot had no `leases` field.
The same run logged `SQLite alarm overdue` and later requests received
`connection_not_found` or expired-session errors. `npm audit
--audit-level=moderate` reported four vulnerabilities, three moderate and one
high, through `undici 7.0.0 - 7.28.0`; the command named
`@cloudflare/vitest-pool-workers 0.20.2`, `wrangler 4.119.0`, and Miniflare as
affected dependents. `npx wrangler deploy --dry-run` passed with Wrangler
4.119.0. Unity 6000.5.1f1 compiled successfully and ran the full Coordination
EditMode suite: 39/40 passed, 1 failed, 0 skipped, exit code 2. The failing
test was `CoordinationProtocolTests.AcceptsEveryV1ServerMessageWithItsRequiredFields`:
the valid `lease.denied` envelope with `"currentLease":null` parsed as false.
The license was valid for this run. Slice 06 is paused pending 05A and 05B.

2026-08-08: Slice 05A committed as
`aa7ad8171be0dac7375f92706771d9649e3f444f`
(`fix(coordination): stabilize authoritative backend`). State tests now derive
their start time from the current run. All multi-statement state transitions,
state-version increments, and replay inserts run in `storage.transactionSync`;
alarms and socket work occur after commit. Expiry now removes sockets with
`4001` before broadcasting removal events only to remaining connections.
Verification from `Tools/CoordinationServer`: `npm ci --ignore-scripts`,
`npm run typecheck`, and `npm test` passed (77/77); focused auth, state, and
WebSocket suites passed 6/6, 10/10, and 12/12. `npm audit
--audit-level=moderate` reported zero vulnerabilities and
`npx wrangler deploy --dry-run` passed with Wrangler 4.120.0. The lockfile
contains Worker pool 0.20.3, Wrangler 4.120.0, Miniflare
5.20260801.1-alpha, and Undici 7.29.0. Remaining risk: this is a deployment
dry run only; Slice 05B must clear the Unity protocol and transport gate before
Slice 06 may resume.

2026-08-08: Slice 05B committed as
`d47f2d0df5418de02ea4a188348295fca59eccb6`
(`fix(coordination): harden Unity connection service`). It accepts explicit
`lease.denied.currentLease: null`, bounds fragmented text messages to 16 KiB,
serializes sends, closes binary and oversized messages with `1003` and `1009`,
starts configured heartbeats only after `session.ready`, and cancels heartbeat
and reconnect work on shutdown or credential removal. Close `4001` creates a
new session immediately; close `4003` sets `AuthenticationFailed`, raises
revocation, and does not retry. Mutation APIs now return correlated request
handles; stale replay responses complete their matching request without
reapplying state. Credential Manager read failures are surfaced without opening
the credential prompt. Unity 6000.5.1f1 ran the full Coordination EditMode
suite successfully: 53/53 passed, 0 failed, 0 skipped, with no new compiler
errors in the log. No scene, prefab, package, or project-setting files changed.
Both stabilization gates are green. Slice 06 remains intentionally paused and
was not started in this scope. Remaining risk: the Worker has only a deployment
dry run and the two-machine Slice 09 acceptance is still outstanding.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Backend and Unity verification completed
- [ ] Two-machine acceptance evidence recorded
- [ ] Required evergreen documentation updated after release acceptance
- [ ] Branch committed and ready for review or merge

## Notes
