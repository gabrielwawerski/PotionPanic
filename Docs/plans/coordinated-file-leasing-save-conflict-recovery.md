---
title: Coordinated Save-Conflict Recovery
status: active
---

# Coordinated Save-Conflict Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a developer keep working, override and save, discard local scene
changes, or move those changes to a new recovery branch when another developer
owns the scene's editing lease.

**Architecture:** Replace the native three-button conflict prompt with a custom
modal editor window. Keep path-global coordination unchanged. Editor-only
services handle destructive scene reload, Git branch creation, and scene Save As
behind injected interfaces so the coordinator contains only decision routing and
pending-save lifecycle logic.

**Tech Stack:** Unity 6000.5.1f1 editor APIs, C#, NUnit EditMode tests, local
Git command-line client, VitePress documentation.

## Global Constraints

- Start implementation from a short-lived feature branch. Do not implement on
  `master`.
- Preserve `OnWillSaveAssets` callback safety. UI, Git processes, scene reload,
  and Save As must run only through the existing deferred scheduler.
- Support loaded `.unity` scenes only. Do not add Prefab Stage, generic asset,
  backend, protocol, Worker, or branch-scoped-lease behavior.
- Keep leases unique by canonical asset path across every Git branch. A recovery
  branch never authorizes saving the original scene path.
- Save recovery scenes under `Assets/CoordinationRecovery/`, outside the current
  `Assets/Scenes/**/*.unity` coordination rule.
- Runtime recovery code must not stage, commit, stash, push, reset, delete, or
  overwrite Git data.
- Branch creation intentionally leaves all staged, unstaged, and untracked files
  in the working tree.
- Do not edit `Assets/Scenes/*.unity`, prefabs, `ProjectSettings`, or `Packages`
  while implementing or testing this slice unless the user explicitly approves
  that exact asset change.
- Keep the plan active until implementation and PP-7 manual acceptance evidence
  are recorded. Do not infer the two-machine gate from automated tests.

---

## Accepted User Flow

The custom modal conflict window displays the affected scene path, last known
owner, and these four actions:

1. `Override and save`
2. `Keep working`
3. `Move to recovery branch`
4. `Discard local changes`

Closing the window is identical to `Keep working`.

Selecting `Move to recovery branch` reveals an in-window recovery form with:

- Editable suggestion `recovery/<scene-slug>-<yyyyMMdd-HHmmss>` using UTC.
- Recovery-path preview under `Assets/CoordinationRecovery/`.
- A warning that every staged, unstaged, and untracked change remains present
  after checkout.
- `Create branch and move scene` and `Back` actions.

On success, the tool creates and checks out the branch, saves the dirty scene as
the active scene at the recovery path, leaves the original scene unchanged on
disk, and reports that no commit was created. The developer must review and
commit before another checkout because uncommitted files are not permanently
attached to a branch.

Selecting `Discard local changes` requires a second confirmation stating that
the scene will be reloaded from disk and the discarded changes cannot be undone.

## Interfaces

Replace the enum-only dialog response with:

```csharp
public enum SaveConflictAction
{
  OverrideAndSave,
  KeepWorking,
  MoveToRecoveryBranch,
  DiscardLocalChanges
}

public sealed class SaveConflictDecision
{
  public SaveConflictAction Action { get; }
  public string RecoveryBranchName { get; }
}
```

Update `ISaveConflictDialog.Show` to accept the suggested branch name and return
`SaveConflictDecision`. Add these editor-only service contracts:

```csharp
public interface ICoordinationGitRecoveryService
{
  string SuggestBranchName(string scenePath);
  bool TryValidateNewBranch(string branchName, out string error);
  bool TryCreateAndSwitch(string branchName, out string error);
}

public interface ICoordinationRecoverySceneSaver
{
  bool TryMoveToRecovery(
    string originalScenePath,
    string branchName,
    out string recoveryScenePath,
    out string error);
}

public interface ICoordinationSceneDiscarder
{
  bool TryDiscard(string scenePath, out string error);
}
```

Add a result object for the composed branch-and-scene operation. It must expose
`BranchCreated`, `SceneMoved`, `BranchName`, `RecoveryScenePath`, and `Error` so
the UI can distinguish a complete failure from a branch-created/Save-As-failed
partial result.

## Task 1: Replace the native conflict prompt with a four-action modal

**Files:**

- Modify: `Assets/Scripts/Editor/Coordination/SaveConflictDialog.cs`
- Create: `Assets/Scripts/Editor/Coordination/SaveConflictWindow.cs`
- Modify: `Assets/Tests/EditMode/Coordination/SaveConflictDialogTests.cs`

- [ ] **Step 1: Write failing decision and controller tests**

  Add tests proving all four actions, window close to `KeepWorking`, editable
  recovery branch input, `Back`, and rejected discard confirmation. The recovery
  decision must contain the final branch text; other decisions must return an
  empty branch name.

- [ ] **Step 2: Run the focused tests and capture RED**

  Run `PotionPanic.Tests.EditMode.Coordination.SaveConflictDialogTests` in the
  Unity Test Runner. Expected: compilation fails because
  `SaveConflictDecision`, `MoveToRecoveryBranch`, `DiscardLocalChanges`, and the
  window controller do not exist.

- [ ] **Step 3: Implement the modal window and decision adapter**

  Implement the custom `EditorWindow` as a modal utility. Separate its state
  transitions from IMGUI drawing so tests can drive main, recovery-form,
  discard-confirmation, success, and failure states without opening real UI.
  Closing any nonterminal window returns `KeepWorking`. Keep the existing path
  and last-known-owner message builder as the single source for conflict copy.

- [ ] **Step 4: Run the focused tests and capture GREEN**

  Re-run the dialog tests. Expected: every decision, close, Back, validation,
  and discard-confirmation case passes.

- [ ] **Step 5: Commit Task 1 only**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/SaveConflictDialog.cs Assets/Scripts/Editor/Coordination/SaveConflictWindow.cs Assets/Tests/EditMode/Coordination/SaveConflictDialogTests.cs
  git commit -m "feat(coordination): add save conflict recovery window"
  ```

## Task 2: Add safe local Git branch creation

**Files:**

- Create: `Assets/Scripts/Editor/Coordination/CoordinationGitRecovery.cs`
- Create: `Assets/Tests/EditMode/Coordination/CoordinationGitRecoveryTests.cs`

- [ ] **Step 1: Write failing Git-service tests**

  Cover the UTC suggestion for `SampleScene.unity`, local syntax rejection,
  `git check-ref-format --branch`, an existing local branch, the exact
  `git switch -c <name>` invocation, missing Git, nonzero exit codes, and error
  output. Include a case where porcelain status contains staged, unstaged, and
  untracked entries; validation must still allow branch creation.

- [ ] **Step 2: Run the focused tests and capture RED**

  Run `PotionPanic.Tests.EditMode.Coordination.CoordinationGitRecoveryTests`.
  Expected: compilation fails because the Git recovery service is absent.

- [ ] **Step 3: Implement process isolation and validation**

  Use an injected process runner with `UseShellExecute = false`, hidden windows,
  redirected output/error, a bounded timeout, and the repository root as the
  working directory. Before Git execution, accept only 1-128 characters from
  `[A-Za-z0-9._/-]`, require an alphanumeric first character, and reject `..`,
  `//`, `@{`, `.lock` suffixes, and trailing `.`, `-`, or `/`. Revalidate
  immediately before `git switch -c`. Never interpolate an unvalidated branch
  name into the command line.

- [ ] **Step 4: Run focused tests and capture GREEN**

  Re-run the Git recovery tests. Expected: all validation, collision, command,
  timeout, and dirty-worktree cases pass.

- [ ] **Step 5: Commit Task 2 only**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/CoordinationGitRecovery.cs Assets/Tests/EditMode/Coordination/CoordinationGitRecoveryTests.cs
  git commit -m "feat(coordination): create local recovery branches"
  ```

## Task 3: Add target-only discard and recovery Save As

**Files:**

- Create: `Assets/Scripts/Editor/Coordination/CoordinationSceneRecovery.cs`
- Create: `Assets/Tests/EditMode/Coordination/CoordinationSceneRecoveryTests.cs`

- [ ] **Step 1: Write failing scene-operation tests**

  Create temporary scene assets in a test-only folder. Verify discard reloads
  only the requested loaded scene and preserves another loaded dirty scene and
  active-scene selection. Verify recovery Save As leaves the original file
  unchanged, opens the recovery scene, preserves other loaded scenes, creates a
  visible `.meta`, and selects deterministic `-2`, `-3` filename suffixes
  instead of overwriting an existing recovery asset.

  Add rejection cases for an empty path, an unloaded scene, a non-`.unity`
  asset, a path outside `Assets/`, and an invalid branch name.

- [ ] **Step 2: Run the focused tests and capture RED**

  Run `PotionPanic.Tests.EditMode.Coordination.CoordinationSceneRecoveryTests`.
  Expected: compilation fails because the scene recovery services are absent.

- [ ] **Step 3: Implement discard and Save As**

  For discard, capture the active scene, close only the target without saving,
  reopen its existing path, and restore the prior active scene when still
  loaded. For recovery, derive
  `Assets/CoordinationRecovery/<branch-slug>/<scene-name>.unity`, create only
  the required directory, and call
  `EditorSceneManager.SaveScene(scene, recoveryPath, false)` so the recovered
  scene becomes active at the new path. Do not call a bulk save API or modify
  Build Settings.

  If Unity reports failure, return the error and leave every resulting file or
  branch for manual inspection. Do not delete partial output automatically.

- [ ] **Step 4: Run the focused tests and capture GREEN**

  Re-run the scene recovery tests. Expected: target isolation, original-file
  preservation, active-scene behavior, suffixing, and rejection cases pass.

- [ ] **Step 5: Commit Task 3 only**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/CoordinationSceneRecovery.cs Assets/Tests/EditMode/Coordination/CoordinationSceneRecoveryTests.cs
  git commit -m "feat(coordination): preserve conflicted scene work"
  ```

## Task 4: Route decisions and refresh branch context

**Files:**

- Modify:
  `Assets/Scripts/Editor/Coordination/CoordinationSaveResumeCoordinator.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationWindowViewModel.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationSaveGuardTests.cs`
- Modify:
  `Assets/Tests/EditMode/Coordination/CoordinationWindowViewModelTests.cs`

- [ ] **Step 1: Write failing orchestration tests**

  Prove `OverrideAndSave` alone sends `lease.override`. Prove `KeepWorking`
  completes the pending path without saving. Prove discard completes the pending
  path and calls only the discarder. Prove recovery completes the original
  pending path before Git or Save As, then calls Git followed by Save As without
  sending acquire, override, release, or uncoordinated-save requests.

  Cover Git failure with no scene save, Save As failure with
  `BranchCreated == true`, successful result copy, and immediate Coordination
  window branch refresh. Verify the next coordination mutation reads the new
  branch through the existing live Git context.

- [ ] **Step 2: Run focused tests and capture RED**

  Run `CoordinationSaveGuardTests` and `CoordinationWindowViewModelTests`.
  Expected: failures show the old enum-only dialog and missing recovery
  dependencies.

- [ ] **Step 3: Implement orchestration and composition**

  Inject Git recovery, scene recovery, discard, result-feedback, and the
  existing clock into the coordinator through `CoordinationBootstrap`.
  Revalidate the branch name before mutation. Call `CompletePath(save, path)`
  before discard or recovery. If Git fails, report failure and keep the dirty
  original. If Git succeeds and Save As fails, report the partial state and
  leave the new branch checked out. On complete success, report the branch,
  recovery path, and uncommitted-work warning.

  Publish a local branch-changed notification after successful checkout. The
  window view model refreshes its cached branch from `ICoordinationGitContext`;
  protocol serialization remains unchanged because it already calls
  `GetBranch()` for each contextual mutation.

- [ ] **Step 4: Run focused and full automated gates**

  Run dialog, Git recovery, scene recovery, save guard, window view-model, and
  service focused tests, followed by the complete Coordination EditMode suite.
  Expected: zero failed, skipped, or inconclusive tests and no new compiler
  warnings.

- [ ] **Step 5: Commit Task 4 only**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/CoordinationSaveResumeCoordinator.cs Assets/Scripts/Editor/Coordination/CoordinationBootstrap.cs Assets/Scripts/Editor/Coordination/CoordinationWindowViewModel.cs Assets/Tests/EditMode/Coordination/CoordinationSaveGuardTests.cs Assets/Tests/EditMode/Coordination/CoordinationWindowViewModelTests.cs
  git commit -m "feat(coordination): route save conflict recovery"
  ```

## Task 5: Update documentation and run manual acceptance

**Files:**

- Modify: [Docs/guides/coordinated-leasing.md](../guides/coordinated-leasing.md)
- Modify: [Docs/tickets/PP-7.md](../archive/tickets/PP-7.md)
- Modify: this plan after evidence is known

- [ ] **Step 1: Update evergreen guidance after implementation**

  Follow [Docs/evergreen-documentation.md](../evergreen-documentation.md). Mark the four-action conflict window
  as current only after code and tests pass. Explain the normal recovery flow,
  the fact that every working-tree change follows checkout, the absence of an
  automatic commit, partial branch-created/Save-As-failed recovery, and the need
  to commit before switching again.

- [ ] **Step 2: Run the single-machine Unity smoke**

  With a synthetic or controlled remote claim, reject discard once, confirm it
  once, close the modal once, and complete a recovery move with staged,
  unstaged, and untracked files present. Verify the branch, recovery scene,
  original scene bytes, unrelated dirty scene, Coordination window branch,
  Console, and `git status`.

- [ ] **Step 3: Run the two-machine smoke**

  Give Machine B the original editing lease. On Machine A, move the dirty scene
  to a recovery branch. Verify Machine B retains the original lease, Machine A
  opens the recovery scene, the original scene stays unchanged, no override or
  release occurs, and both machines show the expected path-global state.

- [ ] **Step 4: Record exact evidence**

  Append the Unity version, branch names, machine roles, different-network
  conditions, paths, test counts, Console result, and failures to PP-7. Exclude
  developer tokens, session tokens, secrets, authorization headers, Credential
  Manager data, and raw local settings. Keep any unperformed manual row open.

- [ ] **Step 5: Verify documentation and handoff state**

  ```powershell
  npm run docs:build
  git diff --check
  git status --short
  ```

  Expected: documentation builds, the diff has no whitespace errors, no test
  scene or generated Unity folder is staged, and only the intended slice files
  remain for handoff.

## Plan Self-Review

- The plan preserves the existing path-global lease contract and requires no
  protocol or Worker change.
- The custom modal is necessary because Unity's native complex dialog exposes
  only three actions.
- Branch creation, scene recovery, discard, routing, and documentation each have
  a separate RED/GREEN gate.
- Every destructive action requires explicit confirmation, and no failure path
  automatically deletes or rolls back user data.
- The plan does not claim that automated evidence completes PP-7's external
  two-machine acceptance gate.

## Execution Handoff

Implement this plan as one bounded coordination slice. Preserve unrelated
working-tree changes and do not stage, commit, push, deploy, or modify secrets
outside the explicit task and commit gates above.
