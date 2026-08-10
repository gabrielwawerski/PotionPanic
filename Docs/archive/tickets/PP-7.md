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
archivedAt: '2026-08-10T20:41:23.577Z'
---

## Description

Implement the tracked coordination program for advisory Unity scene and
selected-prefab presence, leases, reservations, conflict-safe saving, and a
Cloudflare Durable Object backend. Execute one linked plan slice per Codex
session, in dependency order.

## Acceptance Criteria

- [x] The accepted behavior and verification criteria in
  [
  `../plans/coordinated-file-leasing-system.md`](../plans/coordinated-file-leasing-system.md)
  are met.
- [x] Two Windows Unity editors can coordinate from different networks without
  exposing developer or session tokens.
- [x] Offline mode preserves local work and manual collaboration remains the
  documented fallback.

## Implementation Plan

Follow the nine linked implementation slices in dependency order. Each session
must record its commit hash, verification output, and handoff result in
Implementation Notes. Do not combine slices or mark the ticket complete before
the release-acceptance slice records two-machine evidence.

## Implementation Notes

2026-08-06: Plan split into nine independent Codex session slices. PP-8 restored
the root documentation test baseline; `npm test` and
`npm run docs:build` pass. No coordination server or Unity client implementation
has started.

2026-08-06: Slice 01 foundations committed as
`d6563ed868b6b359024ba1ec179c683f5a452313`
(`feat(coordination): scaffold backend and configuration`). Commands passed:
`Tools/CoordinationServer`: `npm ci`, `npm run typecheck`, `npm test` (49), and
`npx wrangler deploy --dry-run`; repository root: `npm test` (11) and
`npm run docs:build`. The Unity Coordination EditMode command could not run:
Unity 6000.5.1f1 exited with code 198 before compilation because no valid Editor
license is activated. `npm ci` also reported four Worker dependency audit
findings (three moderate, one high); no dependency upgrade was made in this
slice.

2026-08-06: Slice 02 authentication committed as
`cb539c2` (`feat(coordination): add revocable opaque sessions`). Commands passed
from `Tools/CoordinationServer`: `npm run typecheck`,
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
upgrade, call `openConnection`, deliver returned envelopes, and extend the alarm
handler with live-socket broadcasting without duplicating lease logic.

2026-08-06: Slice 04 WebSocket synchronization committed as `a8dc482`
(`feat(coordination): synchronize authoritative state`). Commands passed from
`Tools/CoordinationServer`: `npm run typecheck`,
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
and local task context, main-thread event dispatch, and explicit offline
mutation rejection. `CoordinationCredentialWindow` is the limited token-entry
and forget surface before Slice 08. Unity EditMode focused service tests passed
9/9; the full Coordination suite compiled and ran 40 tests with 39 passing. The
remaining failure predates Slice 05: `CoordinationProtocolTests.
AcceptsEveryV1ServerMessageWithItsRequiredFields` rejects the valid
`lease.denied` envelope with `currentLease: null` in the Slice 01 parser. No
scene, prefab, package, or project-setting files changed. Handoff: Slice 06 may
subscribe to `CoordinationService` events and its `Try*` protocol methods,
without changing authentication or transport behavior.

2026-08-08: Stabilization baseline recorded before implementation. The backend
typecheck passed. `npm test -- tests/auth` passed 6/6 and
`npm test -- tests/websocket` passed 11/11, but `npm test -- tests/state`
failed 8/9 and the full `npm test` failed 8/75. The failed state tests use fixed
2026-08-07 timestamps, which had already expired on this run; the first failure
was `Target cannot be null or undefined` at
`authoritative-state.test.ts:74` because the snapshot had no `leases` field. The
same run logged `SQLite alarm overdue` and later requests received
`connection_not_found` or expired-session errors. `npm audit
--audit-level=moderate` reported four vulnerabilities, three moderate and one
high, through `undici 7.0.0 - 7.28.0`; the command named
`@cloudflare/vitest-pool-workers 0.20.2`, `wrangler 4.119.0`, and Miniflare as
affected dependents. `npx wrangler deploy --dry-run` passed with Wrangler
4.119.0. Unity 6000.5.1f1 compiled successfully and ran the full Coordination
EditMode suite: 39/40 passed, 1 failed, 0 skipped, exit code 2. The failing test
was
`CoordinationProtocolTests.AcceptsEveryV1ServerMessageWithItsRequiredFields`:
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
contains Worker pool 0.20.3, Wrangler 4.120.0, Miniflare 5.20260801.1-alpha, and
Undici 7.29.0. Remaining risk: this is a deployment dry run only; Slice 05B must
clear the Unity protocol and transport gate before Slice 06 may resume.

2026-08-08: Slice 05B committed as
`d47f2d0df5418de02ea4a188348295fca59eccb6`
(`fix(coordination): harden Unity connection service`). It accepts explicit
`lease.denied.currentLease: null`, bounds fragmented text messages to 16 KiB,
serializes sends, closes binary and oversized messages with `1003` and `1009`,
starts configured heartbeats only after `session.ready`, and cancels heartbeat
and reconnect work on shutdown or credential removal. Close `4001` creates a new
session immediately; close `4003` sets `AuthenticationFailed`, raises
revocation, and does not retry. Mutation APIs now return correlated request
handles; stale replay responses complete their matching request without
reapplying state. Credential Manager read failures are surfaced without opening
the credential prompt. Unity 6000.5.1f1 ran the full Coordination EditMode suite
successfully: 53/53 passed, 0 failed, 0 skipped, with no new compiler errors in
the log. No scene, prefab, package, or project-setting files changed. Both
stabilization gates are green. Slice 06 remains intentionally paused and was not
started in this scope. Remaining risk: the Worker has only a deployment dry run
and the two-machine Slice 09 acceptance is still outstanding.

2026-08-08: Slice 06 scene and selected-prefab tracking committed as
`df4a9197d5a67b139f5d15fe0b8d876407bb3bae`
(`feat(coordination): track coordinated scenes and prefabs`). The editor-only
lifecycle adapter inventories loaded and additive scenes plus the selected
Prefab Stage, suppresses duplicate transitions, and supports clean teardown and
domain-reload-style reconstruction. It tracks Unity scene-handle identity
independently of asset path, opens coordination when an untitled scene receives
its first valid `Assets/...` save, and reconciles Save As by closing the old
coordinated path before opening the new path. The tracker evaluates every valid
stage against enabled rules, publishes presence for coordinated stages, acquires
editing leases only for dirty exclusive stages, republishes presence after
`session.ready`, and reacquires only dirty exclusive stages. Close removes
presence and releases only a locally owned editing lease. A correlated grant
that arrives after close is released immediately. Acquire request handles are
associated with the stage activation that issued them, so reopening the same
path clean cannot retain an old activation's late grant. A correlated
`RequestSendFailed` event now removes failed acquire handles from this transient
tracker bookkeeping. The tracker never requests a post-ready snapshot and never
creates `lease.reserve` during close.

`CoordinationService.PresenceReceived` changed from
`Action<CoordinationPresenceRecord[]>` to
`Action<CoordinationServerEnvelope>`, preserving `stateVersion` for the local
authoritative store. The tracker forwards current session-ready, snapshot,
presence update/removal, lease result, and correlated completion envelopes.
Stale replay completions complete their request without mutating local state.
The current unchanged allowlist contains one enabled exclusive rule,
`Assets/Scenes/**/*.unity`. Selected prefab stages are observed by lifecycle
tracking but cause no service mutation because no prefab rule exists.

Unity 6000.5.1f1 verification passed the combined focused lifecycle-adapter and
tracker-integration fixtures 23/23 and the full Coordination EditMode suite
95/95. Both runs had 0 failed, 0 skipped, 0 inconclusive, and no C# compiler
errors or warnings in their final logs. This slice did not observe a live Prefab
Stage, deploy the Worker, run two-machine acceptance, install the Slice 08
editor bootstrap/UI lifecycle, or implement the Slice 07 save interception,
authorization, or offline-confirmation behavior. Those items remain required
before PP-7 can be completed.

2026-08-08: Slice 07 conflict-safe saving implemented under commit subject
`feat(coordination): guard conflicting saves`. `OnWillSaveAssets` now returns
all non-exclusive, disabled, and authoritatively local-owned paths immediately.
It omits only exclusive paths with a remote claim or a pending local claim. The
callback performs normalization, state reads, in-memory pending registration,
and `EditorApplication.delayCall` scheduling only. It does not send protocol
requests, open UI, or call a Unity save API. Deferred work correlates each
request ID with its normalized path set and resumes only the exact path whose
current connected-session state contains the local developer's editing lease.
Cached offline ownership and stale replay grants cannot authorize a save.

Authoritative denial queues `SaveConflictDialog` with exactly `Override and
save`, `Cancel save`, and `Keep working`; only the override action sends
`lease.override`. Outage, reconnect, request timeout, and transport-level
override failure can offer `Save locally without coordination`. The offer is
withdrawn if an outage reconnects before UI opens, and saving requires a second
confirmation that lists every affected path and its last known owner. Successful
uncoordinated paths create memory-only warning records and a Console warning;
partial batch failures do not warn for unsaved paths. Slice 08 receives those
records through `ICoordinationUncoordinatedSaveState` and remains responsible
for the main window and lifecycle bootstrap.

Fresh prerequisite verification passed before implementation. The Coordination
Server passed typecheck; auth, state, WebSocket, and full tests passed 6/6,
10/10, 12/12, and 77/77; `npm audit --audit-level=moderate` reported zero
vulnerabilities; and the Wrangler 4.120.0 deployment dry run passed. Unity
6000.5.1f1 then passed the pre-change Coordination EditMode suite 95/95. Final
Slice 07 focused EditMode coverage passed 29/29, and the final full Coordination
EditMode suite passed 124/124 with zero failed, skipped, or inconclusive tests
and no C# compiler errors or warnings in the final log.

The required manual editor smoke is not complete. A temporary, untracked
editor-only harness compiled and logged `SLICE07_SMOKE_READY`, but the supported
Windows Computer Use helper failed before scene interaction with
`SetIsBorderRequired failed: No such interface supported (0x80004002)`. The
harness was removed, Unity closed cleanly, and no scene, prefab,
`ProjectSettings`, or `Packages` file was written. Remaining risk: the real
`UnityCoordinationSaveInvoker` scene, selected-Prefab-Stage, and general asset
save paths still need the planned editor smoke after UI control is available.
Live Worker deployment and two-machine acceptance remain Slice 09 work.

2026-08-08: Slice 08 coordination UI and lifecycle implemented under commit
subject `feat(coordination): add editor coordination interface`. The editor-only
runtime is the single owner of service startup, stage tracking, the Slice 07
save guard and resume coordinator, warning clearance, filtered notifications,
and shutdown. Compilation start, assembly reload, and editor quit converge on
one idempotent shutdown path. If compilation finishes without a domain reload, a
delayed callback creates one replacement runtime; an imminent assembly reload
cancels that callback and lets the reloaded bootstrap own startup. The shutdown
path uninstalls the save guard and subscriptions, queues presence closure and
locally owned editing-lease release, allows up to two seconds for queued sends,
starts cancellation without waiting for an in-flight startup task, cancels
heartbeat, reconnect, and session work, and then closes the socket with a
bounded wait. Lifecycle teardown clears and restores Unity's synchronization
context around the synchronous reload hook; Unity event cleanup and release
queuing stay on the main thread, while bounded network cleanup cannot deadlock
waiting for that thread. Abrupt termination still relies on authoritative server
expiry and connection-close cleanup.

The `Window > Potion Panic > Coordination` window shows authenticated identity,
the Git branch, editable task context, connection state, presence, editing
leases, reservations, owner/branch/task/expiry details, uncoordinated-save
warnings, and the local Disabled control. It exposes reconnect, reserve,
release, override, canonical-path copy, and credential-forget actions with
connection and ownership-based enablement. Only task context and Disabled are
changed through the existing ignored local settings file; credentials remain in
Windows Credential Manager and session tokens remain in memory. Notifications
are limited to claims, conflicts, overrides, reservations, authentication
failure, and disconnects lasting at least two minutes. Slice 07 retains every
save decision. Editing-lease heartbeat updates do not repeat claim notices, and
notices raised while the Coordination window is closed remain in a bounded
in-memory queue until a window can display them. Manual lease actions also use
the enabled coordination path rules, so the window cannot reserve or override an
asset that tracking and the save guard treat as uncoordinated.

Fresh verification passed. `Tools/CoordinationServer` passed `npm run
typecheck`; focused auth, state, and WebSocket tests passed 6/6, 10/10, and
12/12; the full suite passed 77/77; `npm audit --audit-level=moderate` reported
zero vulnerabilities; and `npx wrangler deploy --dry-run` passed with Wrangler
4.120.0. Unity 6000.5.1f1 passed the full Coordination EditMode suite 140/140,
with zero failed, skipped, or inconclusive tests and no C# compiler errors or
warnings in `Logs/coordination-slice08-final.log`.

The editor smoke invoked the real menu command, found the Coordination window,
and logged `SLICE08_FINAL_WINDOW_READY` with its bound rendered state as
unauthenticated, branch
`coordination-slice-08`, Offline, Disabled false, and empty presence, editing
lease, reservation, and warning lists. It opened
`Assets/Scenes/SampleScene.unity`, entered and exited Play Mode, and logged
`SLICE08_FINAL_SMOKE_COMPLETE errors=0` in
`Logs/coordination-slice08-manual-final.log`. The temporary harness and meta
file were removed, Unity then closed normally, and no scene, prefab,
`ProjectSettings`, or
`Packages` file changed. After the completion marker, Unity emitted its internal
shutdown-only `JobTempAlloc` allocation diagnostic; no error, assertion, or
exception was recorded during the window and Play Mode smoke. The supported
Windows inspector still failed with
`node_repl exec context not found`, so no screenshot evidence exists and the
authenticated/connected populated window states were not exercised against a
live Worker. Live deployment, real credentials, two-machine acceptance, and
evergreen documentation remain Slice 09 work.

2026-08-08: Slice 09 pre-deployment review found release-blocking hardening gaps
despite a clean baseline: backend typecheck, 77/77 Worker tests, dependency
audit, Wrangler dry run, 11/11 documentation tests, VitePress build, and 140/140
Unity Coordination EditMode tests passed. Implementation started on
`fix/coordination-release-hardening`. The approved scope covers project routing,
bounded snapshot chunks and project state, indexed token authentication and
session limits, complete revocation cleanup, cross-runtime Unicode path keys,
Unity request cleanup and metadata limits, credential-triggered reconnect,
Wrangler secret/export declarations, and release documentation. Wrangler is not
authenticated on this machine, so deployment, the exact production URL, issued
developer credentials, and two-machine acceptance remain external release
blockers. PP-7 stays open until those steps have recorded evidence.

2026-08-08: Remaining hardening local gate completed on `master`. Repository
HEAD was `92be418`; the reviewed coordination hardening scope ends at
`bbafa22` (`docs(coordination): document secure Worker operations`). No live
deployment, secret mutation, developer-token operation, or destructive
Cloudflare action ran in this gate.

Backend and root verification passed with exact output captured in
`Logs/task5-backend-root-gate.log`. From `Tools/CoordinationServer`, `npm ci`
exited zero and audited 84 packages, `npm run typecheck` passed, `npm test`
passed 98/98, `npm audit --audit-level=moderate` reported zero vulnerabilities,
and `npx wrangler deploy --dry-run` exited zero with Wrangler 4.120.0 and the
`COORDINATION_OBJECT (CoordinationObject)` Durable Object binding. From the
repository root, `npm test` passed 16/16 and `npm run docs:build` completed. The
post-gate worktree was clean.

Unity 6000.5.1f1 passed the focused
`PotionPanic.Tests.EditMode.Coordination.CoordinationSnapshotAssemblerTests`
fixture 12/12 and the full
`PotionPanic.Tests.EditMode.Coordination` suite 204/204, with zero failed,
skipped, or inconclusive tests. Neither final log contains a C# compilation
error or warning. Evidence paths are
`Logs/coordination-release-focused.xml`,
`Logs/coordination-release-focused.log`,
`Logs/coordination-release-editmode.xml`, and
`Logs/coordination-release-editmode.log`.

The manual editor smoke opened `Assets/Scenes/SampleScene.unity` and the
Coordination window, entered Play Mode for eight seconds, exited Play Mode, and
showed an empty Console. The window state was Identity `Not authenticated`, Git
branch `master`, Connection `Offline`, Disabled false, empty task context, and
empty presence, editing-lease, and reservation lists. The scene was not saved,
and no scene, prefab, `ProjectSettings`, or `Packages` file changed. Evidence is
in `Logs/task5-manual-smoke.log`,
`Logs/unity-smoke-playmode.png`, and
`Logs/unity-smoke-console-shortcut.png`.

Independent GPT-5.6 Terra reviewer `task5_independent_review` reviewed the
coordination scope introduced by `15c96e5`, `13edd35`, `173ded5`, and
`bbafa22` and returned `approved`. The review confirmed atomic snapshot apply,
exact-once request termination, UTF-16 256-unit bounds, the shared C# and
TypeScript Unicode fixture with non-ASCII case preservation, declarative
Wrangler release configuration without legacy migrations, no tracked token, and
a verification-only GitHub workflow.

Read-only Cloudflare verification is captured in
`Logs/task5-cloudflare-readonly.log`. `npx wrangler whoami` confirmed one
authenticated account with Worker access. `npx wrangler deployments list`
reported deployment version `badf6872-b3bf-4d7e-879a-57b125937cbe`, created
`2026-08-08T10:15:29.455Z`. The configured Worker's `/health` returned HTTP 200,
service `potion-panic-coordination`, and a parseable `serverTime`.
`npx wrangler secret list` returned an empty list. The endpoint is deployed and
healthy for its unauthenticated health route, but production authentication is
not configured according to the secret-name listing.

PP-7 remains open. Production `TOKEN_HMAC_KEY` and `ADMIN_TOKEN` secrets have
not been verified as configured, developer tokens have not been issued or
provisioned, and two Windows machines on different networks have not completed
any acceptance-matrix row. Presence and reservation, simultaneous acquire,
conflict/cancel/override, clean close, abrupt termination and 120-second expiry,
network loss and outage fallback, 24-hour session recreation, hibernation,
revocation, and filtered live-tail evidence all still require dated external
observations.

2026-08-08: Task 6 production secret provisioning and deployment completed
through the documented local hidden-prompt procedure. Wrangler 4.120.0 used the
single authenticated account `d6fe2dd4378f5957461c683b9cd7cfbd`.
`npx wrangler secret list` reported the required `ADMIN_TOKEN` and
`TOKEN_HMAC_KEY` secret names without exposing their values. After the operator
rotated both values, `npx wrangler deployments list` reported version
`be75c2fc-ad1b-4877-97bd-ca25a02155d7`, created
`2026-08-08T11:24:00.829Z`, receiving 100 percent of traffic. No matching
`potion-panic-secrets-*.env` temporary file remained under `%TEMP%`.

The configured endpoint remains
`https://potion-panic-coordination.gabriel-wawerski.workers.dev`; no
`coordination.json` change was required. Its `/health` route returned HTTP 200,
service `potion-panic-coordination`, and parseable server time
`2026-08-08T11:24:56.654Z`. No secret value, authorization header, developer
token, opaque session, or Credential Manager content was captured.

At this deployment checkpoint, developer tokens had not been issued or
provisioned, and two Windows machines using Unity 6000.5.1f1 on different
networks had not completed any acceptance-matrix row or filtered
`wrangler tail` observation. PP-7 remained open.

2026-08-08: Machine A provisioning used developer label
`Machine A - MX-DESKTOP`. The operator confirmed that the one-time developer
token was saved through `Window > Potion Panic > Coordination`; the token was
not captured in chat, tool output, a tracked file, or PP-7. Unity 6000.5.1f1 was
running on `MX-DESKTOP`, the issuance terminal had closed, and
`UserSettings/PotionPanic/coordination.local.json` contained no key whose name
matched token, secret, authorization, or credential.

The Coordination window showed identity `Machine A - MX-DESKTOP`, developer ID
`9b41718d-1457-40ec-b897-0e977ef0a904`, branch `master`, and connection state
`Connected`. It showed local presence and a locally owned editing lease for
`Assets/Scenes/SampleScene.unity`, both expiring
`2026-08-08T11:39:35.266Z`, with no reservation. Screenshot evidence is
`Logs/task6-machine-a-connected.png`. This verifies Machine A authentication,
connection, presence, and editing-lease acquisition; it does not independently
verify the opaque session's 24-hour lifetime.

A bounded 25-second `npx wrangler tail --format json` observation returned no
event. Machine B has not been issued or provisioned, and no two-machine
acceptance row is complete.

2026-08-08: The operator deferred Machine B provisioning and the external
acceptance matrix. Task 6 remains open at that boundary; no result is inferred
from the single-machine connection.

The Machine A run also exposed a Coordination-window workflow gap. Manual
actions depend on a separately typed `Asset path`, while presence, editing
lease, and reservation rows are passive and cannot select or act on their path.
The visible `Release` action applies only to a locally owned editing lease. The
current client and server do not provide a manual reservation-cancellation
action; reservations remain until expiry, override, or revocation. Treat row
selection, context actions, and reservation cancellation as explicit follow-up
design and protocol work rather than completed release behavior.

2026-08-08: The `feature/coordination-window-actions` implementation adds
path-oriented row selection, active-stage and Project-selection targets, an
advanced manual-path fallback, contextual row actions, disabled-action helper
text, override confirmation, and the explicit path-only
`reservation.cancel` Protocol v1 request. Cancellation is developer-owned and
uses the existing correlated `lease.released` response; `lease.release`
remains editing-lease and connection-owned.

Local verification ran from `Tools/CoordinationServer` with
`npm run typecheck`, `npm test`, `npm audit --audit-level=high`,
`npx wrangler --version`, and `npx wrangler deploy --dry-run`. Results were a
successful typecheck, 104/104 backend tests, zero vulnerabilities, Wrangler
`4.120.0`, and a successful dry run. Root `npm test` passed 16/16 and
`npm run docs:build` completed. Unity `6000.5.1f1` focused suites passed 41/41
protocol, 42/42 service, and 14/14 window-view-model tests; the full
Coordination EditMode suite passed 212/212.

The manual `Assets/Scenes/SampleScene.unity` Play Mode smoke showed zero Console
errors and rendered the new source controls, target guidance, selectable claim
treatment, local reservation cancellation action, and copy action. A
domain-reload focus regression found during the smoke was corrected; the
persisted task context remained empty on the repeat run. Screenshot evidence is
`Logs/coordination-window-actions-smoke.png`.

The updated Worker was not deployed and live cancellation was deliberately not
sent to the older production protocol. No secret, token, credential,
`coordination.json`, or production Worker mutation occurred. The external
two-machine matrix, the new reservation-cancellation row, and filtered tail
evidence remain incomplete, so Task 6 and PP-7 remain open.

2026-08-08: Task 6 deployment resumed from clean `master` at
`91b73ccfedbc0bb3cae68b6346e5d4564cb9fda6`, aligned with `origin/master`. The
user explicitly authorized deployment from this commit. No
`coordination.json` change was needed because the configured endpoint still
matched the deployed Worker URL.

Pre-deploy verification ran from `Tools/CoordinationServer`: `npm run
typecheck` passed; `npm test` passed 104/104; `npm audit
--audit-level=high` reported zero vulnerabilities; `npx wrangler --version`
reported `4.120.0`; `npx wrangler whoami` confirmed the single authenticated
account `d6fe2dd4378f5957461c683b9cd7cfbd`; `npx wrangler deployments list`
showed the prior production version
`be75c2fc-ad1b-4877-97bd-ca25a02155d7`, created
`2026-08-08T11:24:00.829Z`, receiving 100 percent of traffic; `npx wrangler
secret list` reported the required `ADMIN_TOKEN` and `TOKEN_HMAC_KEY` secret
names without exposing values; and `npx wrangler deploy --dry-run` passed with
the `COORDINATION_OBJECT (CoordinationObject)` Durable Object binding. The
pre-deploy `/health` check returned HTTP 200, service
`potion-panic-coordination`, and parseable server time
`2026-08-08T12:58:36.213Z`.

The authorized mutation was `npx wrangler deploy` only. Wrangler uploaded and
deployed `potion-panic-coordination`, reported Worker startup time `4 ms`, the
unchanged URL
`https://potion-panic-coordination.gabriel-wawerski.workers.dev`, and current
version ID `26f5eba2-29e2-4dcf-af6c-9cbafb0dd226`.

Post-deploy verification returned HTTP 200 from `/health`, service
`potion-panic-coordination`, and parseable server time
`2026-08-08T12:59:12.769Z`. `npx wrangler deployments list` reported version
`26f5eba2-29e2-4dcf-af6c-9cbafb0dd226`, created
`2026-08-08T12:58:51.333Z`, receiving 100 percent of traffic. `npx wrangler
secret list` still reported only the required secret names. A credential-free
live protocol smoke, `POST /v1/projects/potion-panic/sessions` with an empty
JSON body and no `Authorization` header, returned HTTP 401. No secret value,
authorization header, developer token, opaque session token, Credential Manager
content, secret provisioning, secret rotation, developer-token operation,
coordination configuration change, GitHub mutation, or unrelated Cloudflare
change occurred.

No Task 6 two-machine acceptance row is completed by this deployment. Presence
and reservation, reservation cancellation, simultaneous acquire, conflict,
cancel, override, clean close, abrupt termination and 120-150 second expiry,
outage fallback, 24-hour session recreation, hibernation, revocation, and
filtered Wrangler-tail evidence still require dated observations from two
Windows machines using Unity 6000.5.1f1 on different networks. PP-7 remains
open.

2026-08-08: Controlled internal rollout checkpoint after excluding the
two-machine acceptance matrix. The deployment evidence commit
`cdc229f44c91a5b807d6bcbb902187c639cb55eb`
(`docs(coordination): record window actions deployment`) is pushed to
`origin/master`. GitHub Actions run `31259384832` for that commit completed
successfully in the `Deploy Docs` workflow. The coordination-server workflow did
not run for this commit because its path filter excludes ticket-only
documentation changes; the Worker-code verification was rerun locally instead.

Local verification from `Tools/CoordinationServer` passed again: `npm run
typecheck`, `npm test` (104/104), and `npx wrangler deploy --dry-run` all exited
zero. The dry run reported the `COORDINATION_OBJECT
(CoordinationObject)` Durable Object binding.

Live Worker sanity checks against
`https://potion-panic-coordination.gabriel-wawerski.workers.dev` returned three
HTTP 200 `/health` responses and three HTTP 401 responses for unauthenticated
`POST /v1/projects/potion-panic/sessions` requests with an empty JSON body. A
bounded `npx wrangler tail --format json` capture ran during those probes and
produced no captured events before the timeout. No secret value, authorization
header, developer token, opaque session token, Credential Manager content,
secret provisioning, secret rotation, developer-token operation,
`coordination.json` change, GitHub mutation, or unrelated Cloudflare change
occurred.

A temporary Unity batch-mode single-machine smoke harness was created and then
removed without committing it. The expected Credential Manager target
`PotionPanic/Coordination/potion-panic/developer-token` exists, but its value
was not read or displayed. Two Unity `6000.5.1f1` batch attempts reached
`TASK6_RELEASE_SMOKE state=Reconnecting` and did not reach `session.ready` or
any request completion before the bounded runs were terminated. Evidence is in
`Logs/task6-release-unity.log`. This is not counted as a passed Unity live
smoke, and no acceptance row is completed from it.

Release status: the current `master` is suitable only for controlled internal
rollout with PP-7 open. Before declaring final release, complete at least one
interactive single-machine live smoke with the updated Unity client, provision
intended developer credentials through the documented secret channel, capture
filtered Wrangler-tail evidence during a credentialed connection or lease
operation, and later run the full two-machine different-network matrix.

2026-08-08: Cloudflare Worker redeployed from clean `master` at
`0b9073a31ad2aa84ca35ac800f80755fa0279c01`
(`docs(coordination): record controlled rollout checkpoint`), aligned with
`origin/master`. Pre-deploy checks from `Tools/CoordinationServer` passed:
`npm run typecheck`, `npm test` (104/104), `npm audit --audit-level=high`
with zero vulnerabilities, `npx wrangler --version` reporting `4.120.0`,
`npx wrangler whoami` confirming the authenticated account
`d6fe2dd4378f5957461c683b9cd7cfbd`, `npx wrangler secret list` reporting only
`ADMIN_TOKEN` and `TOKEN_HMAC_KEY` secret names, and `npx wrangler deploy
--dry-run` reporting the `COORDINATION_OBJECT (CoordinationObject)` Durable
Object binding. The pre-deploy `/health` check returned HTTP 200, service
`potion-panic-coordination`, and parseable server time
`2026-08-08T14:03:06.229Z`.

The authorized mutation was `npx wrangler deploy` only. Wrangler uploaded and
deployed `potion-panic-coordination`, reported Worker startup time `5 ms`, the
unchanged URL
`https://potion-panic-coordination.gabriel-wawerski.workers.dev`, and current
version ID `727852a4-16cc-4bc8-8c12-c64f672f5d6a`.

Post-deploy verification reported version
`727852a4-16cc-4bc8-8c12-c64f672f5d6a`, created
`2026-08-08T14:03:22.815Z`, receiving 100 percent of traffic. `npx wrangler
versions view 727852a4-16cc-4bc8-8c12-c64f672f5d6a` showed the `fetch`
handler, compatibility date `2026-08-06`, the required `ADMIN_TOKEN` and
`TOKEN_HMAC_KEY` secret names, and the `COORDINATION_OBJECT
(CoordinationObject)` Durable Object binding. The post-deploy `/health` check
returned HTTP 200, service `potion-panic-coordination`, and parseable server
time `2026-08-08T14:03:43.566Z`. A credential-free live protocol check,
`POST /v1/projects/potion-panic/sessions` with an empty JSON body and no
`Authorization` header, returned HTTP 401.

No secret value, authorization header, developer token, opaque session token,
Credential Manager content, secret provisioning, secret rotation,
developer-token operation, `coordination.json` change, GitHub mutation, Unity
file change, or unrelated Cloudflare change occurred. This redeploy does not
complete the interactive single-machine smoke, filtered credentialed tail
evidence, developer provisioning, or the two-machine different-network
acceptance matrix. PP-7 remains open.

2026-08-08: The operator reported a successful two-remote-machine save-conflict
workflow for `Assets/Scenes/SampleScene.unity`. The client identified as Gabro
was denied while Patro owned the editing lease. Both `Cancel save` and `Keep
working` left the local scene values unsaved and intact. Gabro then overrode
Patro's lease successfully; the Editing leases row showed `Gabro (local)` and
the displaced client subsequently received the conflict dialog naming Gabro as
the last known owner. The supplied screenshots are transient task evidence and
do not record machine roles, network conditions, Unity versions, or a full
timestamped acceptance log. Record those details before counting this as a
completed different-network acceptance-matrix row. The remaining matrix rows and
filtered Worker-tail evidence are still open.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Backend and Unity verification completed
- [ ] Two-machine acceptance evidence recorded
- [ ] Required evergreen documentation updated after release acceptance
- [ ] Branch committed and ready for review or merge

## Notes

