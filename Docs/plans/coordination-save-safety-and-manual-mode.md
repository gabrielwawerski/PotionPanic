---
title: Coordination Save Safety and Manual Mode
status: active
---

# Coordination Save Safety and Manual Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task by task.

**Goal:** Make every save of a coordinated Unity asset follow one explicit safety policy, including intentional Manual mode and temporary Coordination failures, while keeping a durable local record of every uncoordinated save until a developer reconciles it.

**Architecture:** `CoordinationSaveResumeCoordinator` remains the save-policy boundary. It classifies each save from current Coordination state, obtains an editing lease where possible, and invokes the existing two-step local-save confirmation only for approved fallback states. A new local store records successful uncoordinated saves by normalized asset path. `CoordinationWindowViewModel` exposes the user-facing Coordinated and Manual modes and requires explicit reconciliation of each warning.

**Tech Stack:** Unity 6000.5.1f1, C# editor assemblies, Unity Test Framework EditMode tests, JSON under `UserSettings/`, VitePress documentation, npm verification scripts.

## Global Constraints

- Implement from commit `c397c95` or a later clean `master` that already contains it. Use a short-lived `fix/coordination-outage-policy` branch.
- Do not change the Coordination Worker, Durable Object, HTTP or WebSocket protocol, credentials, scenes, prefabs, packages, or project settings.
- Preserve the serialized `CoordinationUserSettings.disabled` field and the service's internal disabled state. Present those internals as **Manual** in developer-facing UI and documentation so existing local settings do not require migration.
- A coordinated asset may save without a warning only when the current connection owns its editing lease, or when the save resumes through the existing path-scoped, one-shot authorization after both fallback confirmations.
- A fallback save does not establish ownership, update server history, or prove that conflicting changes do not exist.
- Keep manual work announcements mandatory. Reservations communicate planned work; editing leases represent currently open shared assets.
- Do not implement this plan in parallel with [Discard Local Scene Changes on Save Conflict](./coordinated-file-leasing-discard-local-changes.md). Both plans modify the coordinator, save dialog, tests, and guide. Complete this safety foundation first, then rebase the discard-local-changes work.
- Follow test-driven development. Add each failing EditMode case before changing production behavior.
- Unity batch test commands must omit `-quit`; wait for the process to exit, then inspect both the XML result and editor log.
- Never place developer tokens, session tokens, admin secrets, or authorization headers in the warning store, tests, logs, or documentation.
- Do not archive PP-9 or PP-7 as part of this implementation. PP-7 still requires its separate release and two-machine acceptance evidence.

## Decision Record

The approved user-facing modes are:

| Mode | Meaning | Network behavior | Save behavior |
| --- | --- | --- | --- |
| **Coordinated** | The developer expects Coordination to protect shared assets. | Connect, maintain presence, and acquire editing leases. | Save silently only with the local editing lease. Use the guarded fallback for eligible failures. |
| **Manual** | The developer intentionally opts out of live Coordination. | Close the connection and release connection-owned state. Existing reservations may remain until released or expired. | Every coordinated-asset save uses the guarded fallback and creates a durable warning. |

The save policy is:

| State at save time | Required result |
| --- | --- |
| Connected with an authoritative local editing lease | Save immediately. |
| Connected without a lease and no remote owner | Defer the save, acquire the lease, then resume only the authorized paths. |
| Connected with a remote owner | Offer override, cancel, or keep working. Do not offer a direct local-save bypass. |
| Offline | Offer the two-step fallback with `Offline` as the recorded reason. Cancel the fallback if reconnection occurs before the prompt is shown. |
| Reconnecting | Offer the two-step fallback with `Reconnecting` as the recorded reason. Cancel the fallback if reconnection completes before the prompt is shown. |
| Authentication failed | Offer the two-step fallback with `AuthenticationFailed` and show the authentication cause. |
| Manual | Offer the two-step fallback with `Manual`. Do not attempt a network request. |
| Lease request timed out | Offer the two-step fallback with `RequestTimeout`. |
| Override request could not be sent or completed | Offer the two-step fallback with `OverrideTransportFailure`. |
| Initial acquire send fails while the service still reports Connected | Keep the asset dirty and allow retry. Do not convert a transient send failure into an immediate bypass. |

The fallback remains two separate decisions:

1. Continue without Coordination, after seeing the cause and affected assets.
2. Confirm the local save, after seeing that the save can conflict and will create a durable reconciliation warning.

## File Map

### New files

- `Assets/Scripts/Editor/Coordination/CoordinationUncoordinatedSaveStore.cs`
  - Stable reason enum, persisted record model, storage interface, JSON file store, in-memory warning state, and per-path reconciliation.
- `Assets/Tests/EditMode/Coordination/CoordinationUncoordinatedSaveStoreTests.cs`
  - Round-trip, path upsert, atomic replacement, malformed-file quarantine, failure retention, reconciliation, and secret-exclusion coverage.

### Modified runtime and editor files

- `Assets/Scripts/Editor/Coordination/CoordinationSaveResumeCoordinator.cs`
  - Apply the state matrix, pass structured fallback reasons, and record only successful resumed fallback saves.
- `Assets/Scripts/Editor/Coordination/SaveConflictDialog.cs`
  - Show cause-specific first and second confirmations without exposing a one-click bypass for remote ownership conflicts.
- `Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs`
  - Construct the local store and warning state, inject branch and task metadata providers, and retain warning state across connection lifecycle changes.
- `Assets/Scripts/Editor/Coordination/CoordinationWindowViewModel.cs`
  - Add Coordinated and Manual mode commands, warning presentation, persistence errors, and explicit reconciliation.
- `Assets/Scripts/Editor/Coordination/CoordinationWindow.cs`
  - Replace the Disabled checkbox with a Mode selector and render actionable warning records.
- `Assets/Scripts/Editor/Coordination/CoordinationUncoordinatedWarningController.cs`
  - Remove this lifecycle-cleared warning controller after all callers use the durable warning state.

### Modified tests

- `Assets/Tests/EditMode/Coordination/CoordinationSaveGuardTests.cs`
  - State matrix, confirmation sequence, dirty-state preservation, reason metadata, and one-shot resume authorization.
- `Assets/Tests/EditMode/Coordination/CoordinationWindowViewModelTests.cs`
  - Mode transitions, confirmation behavior, display terminology, record rendering, and reconciliation.
- `Assets/Tests/EditMode/Coordination/CoordinationLifecycleTests.cs`
  - Verify close, reconnect, and lease acquisition do not clear warnings.
- `Assets/Tests/EditMode/Coordination/CoordinationServiceTests.cs`
  - Preserve internal disabled-state behavior and connection-owned cleanup expectations.
- `Assets/Tests/EditMode/Coordination/CoordinationUserSettingsTests.cs`
  - Verify the existing `disabled` field still round-trips without migration.

### Modified documentation and work records

- `Docs/guides/coordinated-leasing.md`
- `Docs/onboarding/getting-started.md`
- `Docs/collaboration/team-workflow.md`
- `Tools/CoordinationServer/README.md`
- `Docs/tickets/PP-9.md`

## Task 1: Add the Durable Uncoordinated-Save Record

**Files:**

- Create: `Assets/Scripts/Editor/Coordination/CoordinationUncoordinatedSaveStore.cs`
- Create: `Assets/Tests/EditMode/Coordination/CoordinationUncoordinatedSaveStoreTests.cs`

### Step 1: Define the stable record contract in failing tests

Add tests that require these stable reason values:

```csharp
internal enum CoordinationUncoordinatedSaveReason
{
  Manual,
  Offline,
  Reconnecting,
  AuthenticationFailed,
  RequestTimeout,
  OverrideTransportFailure,
}
```

Require one record per normalized asset path with:

```text
path
firstSavedAtUtc
latestSavedAtUtc
saveCount
reason
lastKnownOwner
branch
task
```

Cover these behaviors before implementation:

- a new path creates a record with count `1`;
- a later save of the same normalized path preserves `firstSavedAtUtc`, updates the latest fields, and increments the count;
- two different paths create two records;
- JSON round-trips every supported reason as its stable string name;
- serialized output contains no property for a token, secret, credential, or authorization header.

### Step 2: Run the focused tests and confirm the expected failure

Run the new EditMode test fixture with Unity 6000.5.1f1. Omit `-quit`. Record the command, XML path, log path, and the missing-type or missing-behavior failure in PP-9.

### Step 3: Implement the model and injectable store

Add:

```csharp
internal interface ICoordinationUncoordinatedSaveStore
{
  CoordinationUncoordinatedSaveLoadResult Load();
  CoordinationUncoordinatedSaveWriteResult Save(
    IReadOnlyList<CoordinationUncoordinatedSaveRecord> records);
}
```

The production store must use:

```text
UserSettings/PotionPanic/coordination-uncoordinated-saves.json
```

Use schema version `1`. Write a temporary file in the same directory, flush and close it, then replace the destination. Do not write directly over the only valid copy.

Allow tests to inject the destination path and clock. Tests must use their own temporary directory and remove it during teardown.

### Step 4: Handle malformed and failed persistence explicitly

Add failing tests, then implement:

- malformed JSON is moved to `coordination-uncoordinated-saves.invalid-<UTC timestamp>.json`;
- the load result reports the quarantine path and starts with an empty record set;
- a failed write leaves the warning in memory and exposes a persistent error string;
- reconciliation removes a record only after the updated record set has been saved successfully;
- reconciliation write failure leaves the record visible.

The state object must not discard warnings because Unity closes a window, the service reconnects, or a lease is later acquired.

### Step 5: Run the focused fixture and review the serialized sample

Confirm the fixture passes. Inspect one generated JSON sample and search it for `token`, `secret`, `credential`, `authorization`, and `bearer` before deleting the test directory.

### Step 6: Commit the storage slice

Stage only the new store and its tests.

```powershell
git add -- Assets/Scripts/Editor/Coordination/CoordinationUncoordinatedSaveStore.cs Assets/Tests/EditMode/Coordination/CoordinationUncoordinatedSaveStoreTests.cs
git commit -m "feat(coordination): persist uncoordinated save warnings"
```

## Task 2: Apply One Save Policy to Every Coordination State

**Files:**

- Modify: `Assets/Scripts/Editor/Coordination/CoordinationSaveResumeCoordinator.cs`
- Modify: `Assets/Scripts/Editor/Coordination/SaveConflictDialog.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationSaveGuardTests.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationLifecycleTests.cs`

### Step 1: Add failing state-matrix tests

Add one focused test per matrix row. Assert both the immediate save result and the final dirty state.

Required cases:

- authoritative local lease saves without a prompt;
- no lease defers, acquires, and resumes only the original normalized paths;
- a remote owner offers override, cancel, or keep working and never calls the uncoordinated-save prompt;
- Offline, Reconnecting, AuthenticationFailed, Manual, RequestTimeout, and OverrideTransportFailure call both confirmation steps and record the exact reason;
- declining either confirmation leaves every affected asset dirty;
- reconnection before the fallback prompt cancels the fallback and retries the coordinated path;
- an acquire send failure while still Connected leaves the asset dirty without offering fallback;
- accepting fallback authorizes one save attempt for the exact paths and cannot authorize a later save;
- a mixed batch cannot use authorization from one path to save another path;
- the warning record is added only after the resumed save succeeds;
- the record includes last known owner, current branch, and current task when available;
- absent metadata is stored as empty, not invented.

### Step 2: Run the focused guard tests and confirm failures

Run only `CoordinationSaveGuardTests`. Confirm the new Manual and authentication cases fail under the old early-return behavior and that the metadata assertions fail before production changes.

### Step 3: Pass structured reasons through the dialog

Replace generic boolean-only fallback calls with a structured request containing:

```csharp
internal sealed class CoordinationUncoordinatedSaveRequest
{
  public CoordinationUncoordinatedSaveReason Reason { get; init; }
  public IReadOnlyList<string> AssetPaths { get; init; }
  public string Detail { get; init; }
}
```

The first dialog explains why Coordination cannot authorize the save. The second dialog names the affected paths, states that conflicts are still possible, and states that a local reconciliation warning will remain. Authentication failures must include the available authentication cause without displaying credentials.

Do not route the connected remote-owner branch through this request. Its choices remain override, cancel, or keep working.

### Step 4: Implement the state matrix in the coordinator

Remove the save guard's current unconditional pass-through for the internal disabled state. Map it to `Manual` and use the same path-scoped, one-shot resume mechanism as other eligible fallback states.

Inject:

- the durable warning state;
- a branch provider backed by the existing Git context;
- a task provider backed by the current local settings;
- current owner context from the latest coordination snapshot.

Record a warning only after Unity accepts the resumed save. Preserve the existing stale-callback, batch, owner-change, and path-normalization protections.

### Step 5: Remove lifecycle-based warning clearing

Update bootstrap and lifecycle wiring so that connection close, reconnect, presence loss, lease acquisition, and window close do not clear warnings. Delete `CoordinationUncoordinatedWarningController.cs` only after its responsibilities are covered by the durable warning state.

Add lifecycle tests for close/reopen and reconnect. A full Unity editor restart is covered by Task 5's manual smoke.

### Step 6: Run focused and complete Coordination EditMode suites

Run:

1. `CoordinationSaveGuardTests`;
2. `CoordinationLifecycleTests`;
3. the full `Assets/Tests/EditMode/Coordination` suite.

Inspect XML and logs after each run. Treat a missing XML file, editor crash, compilation error, or timeout as a failed gate.

### Step 7: Commit the save-policy slice

Stage only the coordinator, dialog, bootstrap, retired controller, and affected tests.

```powershell
git add -- Assets/Scripts/Editor/Coordination/CoordinationSaveResumeCoordinator.cs Assets/Scripts/Editor/Coordination/SaveConflictDialog.cs Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs Assets/Scripts/Editor/Coordination/CoordinationUncoordinatedWarningController.cs Assets/Tests/EditMode/Coordination/CoordinationSaveGuardTests.cs Assets/Tests/EditMode/Coordination/CoordinationLifecycleTests.cs
git commit -m "fix(coordination): guard every uncoordinated save"
```

## Task 3: Replace Disabled with Coordinated and Manual Modes

**Files:**

- Modify: `Assets/Scripts/Editor/Coordination/CoordinationWindowViewModel.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationWindow.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationWindowViewModelTests.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationServiceTests.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationUserSettingsTests.cs`

### Step 1: Add failing mode-transition tests

Define the user-facing enum:

```csharp
internal enum CoordinationMode
{
  Coordinated,
  Manual,
}
```

Test that:

- the existing `settings.disabled == true` maps to Manual;
- `settings.disabled == false` maps to Coordinated;
- choosing Manual requires confirmation;
- cancelling confirmation leaves the mode and connection unchanged;
- confirming Manual closes the connection and releases connection-owned state through existing service behavior;
- the confirmation states that reservations may remain;
- choosing Coordinated clears the internal disabled flag and starts the existing connection path;
- the UI never labels the mode or connection state as Disabled;
- the existing `disabled` setting still serializes and reloads unchanged.

Keep the service's internal disabled-state tests. This plan changes developer language, not the established local persistence field or internal state machine.

### Step 2: Add failing warning and reconciliation tests

Require the view model to expose each outstanding record with:

- asset path;
- first and latest save time;
- save count;
- reason;
- last known owner;
- branch;
- task;
- persistence or quarantine error, when present.

Test that `Mark reconciled`:

- requires confirmation for one selected record;
- explains that the action does not merge files or update server history;
- removes only that record after a successful store write;
- keeps the record visible on write failure;
- has no bulk-clear action.

### Step 3: Implement testable confirmation boundaries

Add an injected confirmation interface to the view model for entering Manual mode and marking a record reconciled. Keep Unity modal APIs inside the window adapter so view-model tests do not depend on editor dialogs.

The Manual confirmation must say:

- the live connection will close;
- connection-owned presence and editing leases will be released;
- reservations may remain until released or expired;
- every coordinated-asset save will require two confirmations and create a warning.

### Step 4: Update the window

Replace the Disabled checkbox with a Coordinated/Manual selector. Display internal `ConnectionState.Disabled` as Manual. Show a compact warning section with one reconciliation action per asset and a persistent error panel when loading, quarantine, or saving fails.

Do not add an action that deletes all warnings. Do not imply that entering Coordinated mode resolves existing records.

### Step 5: Run focused and complete Coordination EditMode suites

Run the view-model, service, user-settings, and storage fixtures, followed by the full Coordination EditMode suite. Inspect XML and logs.

### Step 6: Commit the mode and reconciliation slice

```powershell
git add -- Assets/Scripts/Editor/Coordination/CoordinationWindowViewModel.cs Assets/Scripts/Editor/Coordination/CoordinationWindow.cs Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs Assets/Tests/EditMode/Coordination/CoordinationWindowViewModelTests.cs Assets/Tests/EditMode/Coordination/CoordinationServiceTests.cs Assets/Tests/EditMode/Coordination/CoordinationUserSettingsTests.cs
git commit -m "feat(coordination): add explicit manual mode"
```

## Task 4: Align Developer and Operator Documentation

**Files:**

- Modify: `Docs/guides/coordinated-leasing.md`
- Modify: `Docs/onboarding/getting-started.md`
- Modify: `Docs/collaboration/team-workflow.md`
- Modify: `Tools/CoordinationServer/README.md`
- Modify: `Docs/tickets/PP-9.md`

### Step 1: Update the Unity Coordination Guide

Explain the policy from the developer's point of view:

- remain in Coordinated mode during a temporary outage;
- choose Manual only for an intentional, team-agreed opt-out;
- Manual mode does not cancel reservations automatically;
- Offline, Reconnecting, authentication failure, timeout, and Manual saves all use the same two-step fallback;
- the local warning is evidence of unresolved local risk, not server history;
- acquiring a later lease does not reconcile an earlier uncoordinated save;
- reconciliation means the developer compared, merged, reverted, or otherwise resolved the asset with the team, then explicitly retired that record.

Update the quick reference and troubleshooting tables. Keep developer procedures here and operator procedures in the server runbook.

### Step 2: Update setup and daily workflow

In Project Setup, explain the initial Coordinated mode, authentication-failure behavior, and how to stop when credentials or the configured server are wrong.

In Daily Workflow, explain when a team may intentionally choose Manual, how to announce it, how to handle a fallback save, and why a warning survives reconnection and editor restart.

Do not weaken manual work-announcement or reservation guidance.

### Step 3: Update the operator runbook

Describe the boundary precisely:

- the server cannot observe a save made while disconnected or in Manual mode;
- disabling the client releases connection-owned state through the normal disconnect lifecycle, but reservations can outlive the connection;
- the warning ledger lives only in the developer's local `UserSettings` and is not an operator audit log;
- operators should diagnose service and authentication failures without telling developers to clear warnings as a recovery step.

### Step 4: Update PP-9 evidence without closing it

Link this plan from PP-9. Record the approved decisions and the exact automated and manual evidence gathered during implementation. Check acceptance items only when their evidence exists. Leave PP-9 open until the temporary-asset manual smoke passes.

### Step 5: Run repository documentation checks

```powershell
npm test
npm run docs:build
git diff --check
```

Also search the changed documentation for stale developer-facing uses of Disabled:

```powershell
rg -n "Disabled|disabled" Docs/guides/coordinated-leasing.md Docs/onboarding/getting-started.md Docs/collaboration/team-workflow.md Tools/CoordinationServer/README.md
```

Keep legitimate references to the serialized field or internal service state. Replace only user-facing mode language.

### Step 6: Commit the documentation slice

```powershell
git add -- Docs/guides/coordinated-leasing.md Docs/onboarding/getting-started.md Docs/collaboration/team-workflow.md Tools/CoordinationServer/README.md Docs/tickets/PP-9.md
git commit -m "docs(coordination): define manual save recovery"
```

## Task 5: Run Manual Editor Acceptance

**Files:**

- Modify only if evidence is recorded: `Docs/tickets/PP-9.md`
- Create temporarily, then remove: a coordinated test scene and its `.meta` file

### Step 1: Protect the working tree

Start from a clean tree except for the PP-9 evidence edit. Record current Coordination local settings so they can be restored. Announce the temporary shared-asset exercise before creating or opening the test scene.

Do not use `Assets/Scenes/SampleScene.unity` for destructive save experiments.

### Step 2: Verify Manual decline and acceptance

Create a temporary coordinated scene.

1. Enter Manual mode and confirm the mode-transition warning.
2. Dirty the scene and attempt to save.
3. Decline the first fallback prompt; confirm the scene stays dirty.
4. Repeat and decline the second prompt; confirm the scene stays dirty.
5. Repeat and accept both prompts; confirm the scene saves and one warning record appears with reason Manual.

### Step 3: Verify persistence and coordinated fallback

1. Close and reopen the Coordination window; confirm the record remains.
2. Restart Unity; confirm the record remains.
3. Return to Coordinated mode and configure an unreachable endpoint using the existing local test procedure.
4. Repeat the decline and accept flows; confirm the correct unavailable-state reason is recorded.
5. Restore a working connection and acquire the asset's editing lease; confirm neither warning disappears.

Do not print credentials or local settings containing credentials in the ticket evidence.

### Step 4: Verify explicit reconciliation

Use the displayed metadata to compare the temporary asset with the relevant Git and team state. Select one record, accept `Mark reconciled`, and confirm only that record disappears. Restart Unity and confirm it stays cleared while the other record remains.

Reconcile the remaining record explicitly. There must be no bulk-clear shortcut.

### Step 5: Restore and clean up

Restore the developer's original local Coordination settings. Delete the temporary scene and its `.meta` file. Confirm no scene, prefab, package, project-setting, generated Unity folder, credential, or warning-store artifact is staged.

### Step 6: Run final gates

Run:

1. the full Coordination EditMode suite;
2. any affected PlayMode tests;
3. `npm test`;
4. `npm run docs:build`;
5. `git diff --check`;
6. `git status --short`.

Record exact commands, counts, XML/log paths, manual observations, and remaining risks in PP-9. Do not describe the separate two-machine PP-7 gate as complete.

### Step 7: Commit acceptance evidence

```powershell
git add -- Docs/tickets/PP-9.md
git commit -m "docs(coordination): record save safety acceptance"
```

## Completion Criteria

- Every coordinated-asset save follows the decision matrix.
- Manual mode requires an explicit transition confirmation and guarded saves.
- Eligible uncoordinated saves require two confirmations and leave rejected assets dirty.
- Connected remote ownership never exposes a direct local bypass.
- Each successful fallback save creates or updates one durable per-path record.
- Warnings survive window close, reconnect, lease acquisition, and Unity restart.
- Only explicit, successful per-record reconciliation removes a warning.
- The warning file contains no credentials or authorization data.
- Developer-facing UI and documentation use Coordinated and Manual consistently.
- Focused and full Coordination EditMode suites pass with inspected XML and logs.
- Repository tests, documentation build, and `git diff --check` pass.
- The manual temporary-asset smoke passes and leaves no protected or generated artifacts.
- PP-9 contains the evidence and remains open unless every PP-9 acceptance criterion is proven.
- PP-7 remains governed by its separate deployment, revocation, and two-machine acceptance gates.
