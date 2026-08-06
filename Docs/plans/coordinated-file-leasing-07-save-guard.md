---
title: 'Coordinated Leasing 07: Conflict-Safe Save Guard'
---

# Coordinated Leasing 07: Conflict-Safe Save Guard

**Session goal:** Prevent accidental conflicting saves while preserving local
work during pending requests, failures, offline operation, and reloads.

**Depends on:** Slices 05 and 06.

**Produces:** A callback-safe save guard that cancels only conflicted paths and
resumes only the omitted paths after an authoritative grant or override.

## Files

- Create the save-guard and resume-coordinator files under
  `Assets/Scripts/Editor/Coordination/`.
- Add matching tests under `Assets/Tests/EditMode/Coordination/`.

## Implementation steps

- In `AssetModificationProcessor.OnWillSaveAssets`, return all safe paths
  immediately and omit only paths with a remote claim or pending local claim.
  Start asynchronous acquisition or override work after the callback returns.
- Represent each pending save by its normalized target paths and request ID.
  Handle multi-path saves without resuming an unrelated path.
- After an authoritative grant or override, resume only the omitted target via
  `EditorApplication.delayCall`. Use a one-shot recursion guard that clears on
  completion and on failure.
- Show cancel and explicit override outcomes for remote conflicts. If an
  override fails, if the backend is offline, or if the editor reloads, preserve
  dirty local changes and leave the file editable.
- Never treat a timeout or local offline state as ownership. Manual coordination
  remains the fallback.

## Verification

Run focused EditMode tests for remote conflicts, pending claims, multi-path
saves, offline saves without claims, override failure, grant resume, and
recursive resume prevention. Run the full Coordination EditMode suite and a
manual editor smoke test that edits a coordinated scene, cancels a conflict,
and verifies the scene remains dirty when the claim fails.

**Commit:** `feat(coordination): guard conflicting saves`

**Handoff:** Record the callback behavior and test evidence in `PP-7`. Slice 08
may present these states, but must not move save decisions into the window.
