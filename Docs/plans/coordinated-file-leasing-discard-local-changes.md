---
title: Discard Local Scene Changes on Save Conflict
status: active
---

# Discard Local Scene Changes on Save Conflict Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a developer explicitly discard unsaved local changes to a coordinated scene when another developer owns its editing lease.

**Architecture:** Replace `Cancel save`, which currently has the same behavior as `Keep working`, with `Discard local changes`. A second confirmation prevents an accidental reload. The save coordinator calls an injected editor-only discarder that reloads only the affected loaded scene from disk; it does not save, override, or contact the coordination service.

**Tech Stack:** Unity 6000.5.1f1 editor APIs, C#, NUnit EditMode tests.

## Global Constraints

- Limit scope to `.unity` scene paths matched by the existing exclusive rule. Do not add Prefab Stage, asset, protocol, Worker, or scene-file changes.
- Keep `OnWillSaveAssets` callback-safe. The confirmation and reload must run only through its existing deferred path.
- Preserve unrelated loaded scenes and the active scene. A failed reload must leave the user's local changes intact.
- Do not call `SaveScene`, `SaveOpenScenes`, `AssetDatabase.SaveAssets`, or any lease mutation while discarding.
- Do not stage, commit, push, or modify `Assets/Scenes/SampleScene.unity` as part of this slice unless the user explicitly requests it.

---

## File Map

| File                                                                      | Change                                                       |
|---------------------------------------------------------------------------|--------------------------------------------------------------|
| `Assets/Scripts/Editor/Coordination/SaveConflictDialog.cs`                | Add the discard choice and its confirmation.                 |
| `Assets/Scripts/Editor/Coordination/CoordinationSceneDiscarder.cs`        | New target-only Unity scene reloader.                        |
| `Assets/Scripts/Editor/Coordination/CoordinationSaveResumeCoordinator.cs` | Route confirmed discard without saving or sending a request. |
| `Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs`             | Supply the production discarder.                             |
| `Assets/Tests/EditMode/Coordination/SaveConflictDialogTests.cs`           | Test final labels, mapping, and rejected confirmation.       |
| `Assets/Tests/EditMode/Coordination/CoordinationSceneDiscarderTests.cs`   | Test target-only reload and invalid targets.                 |
| `Assets/Tests/EditMode/Coordination/CoordinationSaveGuardTests.cs`        | Test coordinator routing and no-save behavior.               |
| `Docs/guides/coordinated-leasing.md`                                      | Describe the final choice and destructive confirmation.      |
| `Docs/tickets/PP-7.md`                                                    | Record focused test and manual-smoke evidence.               |

## Task 1: Define the confirmed discard dialog

**Files:**

- Modify: `Assets/Scripts/Editor/Coordination/SaveConflictDialog.cs:6-115`
- Modify: `Assets/Tests/EditMode/Coordination/SaveConflictDialogTests.cs:9-68`

**Interfaces:**

- Produce `SaveConflictAction.DiscardLocalChanges`.
- Keep `ISaveConflictDialog.Show(IReadOnlyList<CoordinationSavePathInfo>)`.

- [ ] **Step 1: Write failing tests**

  Change the mapping tests to these final choices and add rejected-confirmation coverage:

  ```csharp
  [TestCase(0, SaveConflictAction.OverrideAndSave)]
  [TestCase(1, SaveConflictAction.KeepWorking)]
  [TestCase(2, SaveConflictAction.DiscardLocalChanges)]
  public void MapsConflictButtonsToTheirActions(int selected,
    SaveConflictAction expected) { /* existing fake backend setup */ }

  [Test]
  public void RejectedDiscardConfirmationKeepsWorking()
  {
    backend.ComplexResult = 2;
    backend.ConfirmationResult = false;
    Assert.That(dialog.Show(Paths()), Is.EqualTo(SaveConflictAction.KeepWorking));
  }
  ```

- [ ] **Step 2: Run the focused test and verify failure**

  In Unity 6000.5.1f1 Test Runner, run
  `PotionPanic.Tests.EditMode.Coordination.SaveConflictDialogTests`. Expected: compile failure because `DiscardLocalChanges` is absent.

- [ ] **Step 3: Implement the dialog**

  Replace `CancelSave` with `DiscardLocalChanges`. Pass `Override and save`,
  `Keep working`, and `Discard local changes` to `ShowComplex`. When the third option is chosen, call `ShowConfirmation` with the existing path/owner list plus this warning: `The listed local scene changes will be reloaded from disk
  and cannot be undone.` Return `KeepWorking` when confirmation is rejected.

- [ ] **Step 4: Run the focused test and verify success**

  Run the same Test Runner filter. Expected: all mapping and confirmation tests pass.

- [ ] **Step 5: Commit Task 1**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/SaveConflictDialog.cs Assets/Tests/EditMode/Coordination/SaveConflictDialogTests.cs
  git commit -m "feat(coordination): add discard choice to save conflict"
  ```

## Task 2: Reload only the requested scene from disk

**Files:**

- Create: `Assets/Scripts/Editor/Coordination/CoordinationSceneDiscarder.cs`
- Create: `Assets/Tests/EditMode/Coordination/CoordinationSceneDiscarderTests.cs`

**Interfaces:**

```csharp
public interface ICoordinationSceneDiscarder
{
  bool TryDiscard(string path);
}
```

- [ ] **Step 1: Write failing target-isolation tests**

  Create two temporary scene assets in each test. Load both, dirty only the target scene, set the other scene active, then call `TryDiscard(targetPath)`. Assert the target value equals its disk value and is clean, while the other scene remains loaded, dirty, unchanged, and active. Add tests that an unloaded target and a non-`.unity` target return `false` without changing a loaded scene.

- [ ] **Step 2: Run the focused test and verify failure**

  In Unity Test Runner, run
  `PotionPanic.Tests.EditMode.Coordination.CoordinationSceneDiscarderTests`. Expected: compile failure because the interface and implementation do not exist.

- [ ] **Step 3: Implement target-only reload**

  Implement `UnityCoordinationSceneDiscarder.TryDiscard`:

    1. Normalize with `CoordinationPathMatcher.TryNormalize`, reject non-scene paths, and locate the exact loaded `Scene` by path.
    2. Capture the active scene path and whether another scene is loaded.
    3. For the only loaded scene, use
       `EditorSceneManager.OpenScene(path, OpenSceneMode.Single)`.
    4. Otherwise call `EditorSceneManager.CloseScene(target, true)` and reopen only that path with `OpenSceneMode.Additive`.
    5. Restore the previous active scene if it remains loaded; otherwise activate the reloaded target. Return `false` on any failed Unity operation.

- [ ] **Step 4: Run the focused test and verify success**

  Run the same Test Runner filter. Expected: all target-only and rejection tests pass without a save prompt.

- [ ] **Step 5: Commit Task 2**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/CoordinationSceneDiscarder.cs Assets/Tests/EditMode/Coordination/CoordinationSceneDiscarderTests.cs
  git commit -m "feat(coordination): discard conflicted scene changes"
  ```

## Task 3: Route discard from the save coordinator

**Files:**

- Modify: `Assets/Scripts/Editor/Coordination/CoordinationSaveResumeCoordinator.cs:119-141,415-448`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs:353-364`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationSaveGuardTests.cs:355-386,476-490`

**Interfaces:**

- Consume `SaveConflictAction.DiscardLocalChanges` and
  `ICoordinationSceneDiscarder.TryDiscard(string path)`.
- Produce a completed pending save path with no Unity save, override, retry, or uncoordinated-save record.

- [ ] **Step 1: Write failing coordinator tests**

  Add a `FakeSceneDiscarder` to `SaveFixture`. Drive a `lease.acquire` denial with `Dialog.Result = SaveConflictAction.DiscardLocalChanges`, then assert:

  ```csharp
  Assert.That(fixture.Discarder.Paths, Is.EqualTo(new[] { Laboratory }));
  Assert.That(fixture.Saves.Paths, Is.Empty);
  Assert.That(fixture.WarningState.Paths, Is.Empty);
  Assert.That(fixture.Service.Requests.Any(x => x.StartsWith("lease.override:")), Is.False);
  ```

  Add a `TryDiscard` false case with the same no-save, no-override, no-retry, and no-warning assertions.

- [ ] **Step 2: Run the focused test and verify failure**

  In Unity Test Runner, run
  `PotionPanic.Tests.EditMode.Coordination.CoordinationSaveGuardTests`. Expected: constructor or enum compilation failure until the dependency is added.

- [ ] **Step 3: Implement coordinator integration**

  Add the discarder as a required constructor dependency, construct
  `UnityCoordinationSceneDiscarder` in `CoordinationBootstrap`, and handle
  `DiscardLocalChanges` in `QueueConflictDialog` by calling `TryDiscard(path)`
  then `CompletePath(save, path)`. Keep the override branch unchanged and map
  `KeepWorking` directly to `CompletePath`.

- [ ] **Step 4: Run focused and full tests**

  Run the three focused classes, then the full Coordination EditMode suite in Unity 6000.5.1f1. Expected: all pass with no new compiler warnings.

- [ ] **Step 5: Commit Task 3**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/CoordinationSaveResumeCoordinator.cs Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs Assets/Tests/EditMode/Coordination/CoordinationSaveGuardTests.cs
  git commit -m "feat(coordination): discard local changes after conflict"
  ```

## Task 4: Update guidance and manually verify Unity behavior

**Files:**

- Modify: `Docs/guides/coordinated-leasing.md:125-140`
- Modify: `Docs/tickets/PP-7.md`

- [ ] **Step 1: Update the guide**

  Replace the Cancel save instruction with `Choose Keep working to leave local
  changes unsaved and intact.` Add: `Choose Discard local changes only when the
  local scene changes are unwanted. Confirming reloads that scene from disk and
  cannot be undone.` Keep override and outage guidance unchanged.

- [ ] **Step 2: Perform the Unity smoke**

  With another developer holding the scene lease, create an unsaved visible change in `Assets/Scenes/SampleScene.unity`. Reject discard confirmation once and confirm the value remains dirty. Repeat and confirm discard, then verify the value matches disk, no override request occurred, and a second dirty scene stays unchanged. Review the Console for errors.

- [ ] **Step 3: Record test evidence**

  Append Unity version, machine role, date, paths, confirmation results, focused/full test counts, and Console result to PP-7. Exclude tokens, sessions, secrets, authorization headers, Credential Manager data, and raw local settings.

- [ ] **Step 4: Verify documentation and diff**

  ```powershell
  npm run docs:build
  git diff --check
  ```

  Expected: both commands succeed.

- [ ] **Step 5: Commit Task 4**

  ```powershell
  git add Docs/guides/coordinated-leasing.md Docs/tickets/PP-7.md
  git commit -m "docs(coordination): document discard conflict handling"
  ```

## Plan Self-Review

- Tasks 1-3 cover the explicit, confirmed, target-only discard path.
- Task 4 covers guidance, manual proof, and evidence recording.
- The plan changes no backend, protocol, Prefab Stage, or scene asset.
- `SaveConflictAction.DiscardLocalChanges` and
  `ICoordinationSceneDiscarder.TryDiscard(string path)` are the only new cross-file contracts.

## Execution Handoff

Execute this as one coordination slice after the operator resolves the current manual-test change in `Assets/Scenes/SampleScene.unity`.
