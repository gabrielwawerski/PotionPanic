---
title: 'Coordinated Leasing 07: Conflict-Safe Save Guard'
---

# Coordinated Leasing 07: Conflict-Safe Save Guard

**Session goal:** Prevent accidental conflicting saves while preserving local
work during pending requests, failures, offline operation, and reloads.

**Depends on:** Slices 05B and 06.

**Produces:** A callback-safe save guard that cancels only conflicted paths and
resumes only the omitted paths after an authoritative grant or override.

## Files

- Create the save-guard and resume-coordinator files under
  `Assets/Scripts/Editor/Coordination/`.
- Create `SaveConflictDialog.cs` under `Assets/Scripts/Editor/Coordination/`.
- Add matching tests under `Assets/Tests/EditMode/Coordination/`.

## Implementation steps

- In `AssetModificationProcessor.OnWillSaveAssets`, return all safe paths
  immediately and omit only paths with a remote claim or pending local claim.
  Start asynchronous acquisition or override work after the callback returns.
- Key each pending save by its request ID and normalized path set. Handle
  multi-path saves without resuming an unrelated path.
- Resume a save only when current authoritative state confirms that the local
  developer owns the editing lease. A stale replayed grant is insufficient.
- Add `Save locally without coordination`, enabled only during an outage,
  reconnect, timeout, or transport-level override failure. Require a second
  confirmation showing affected paths and the last known owner.
- Mark an uncoordinated save in memory, show it in the Coordination UI, and log
  a warning. Do not create backend history or tracked state.
- When an authoritative `lease.denied` result identifies a remote owner, queue
  `SaveConflictDialog` with `EditorApplication.delayCall`; never open UI inside
  `OnWillSaveAssets`. The dialog has exactly `Override and save`, `Cancel save`,
  and `Keep working` actions. Only `Override and save` sends `lease.override`;
  the other actions preserve dirty local changes without scheduling a save. If
  an override fails, if the backend is offline, or if the editor reloads,
  preserve dirty local changes and leave the file editable.
- Preserve dirty work for cancellation, authoritative denial, reload, and failed
  saves. Never treat a timeout or local offline state as ownership. Manual
  coordination remains the fallback.

## Verification

Run focused EditMode tests for remote conflicts, pending claims, multi-path
saves, offline saves without claims, override failure, grant resume, and
recursive resume prevention. Cover the dialog's three actions and confirm it is
created only after the save callback returns. Run the full Coordination EditMode
suite and a manual editor smoke test that edits a coordinated scene, cancels a
conflict, and verifies the scene remains dirty when the claim fails.

**Commit:** `feat(coordination): guard conflicting saves`

**Handoff:** Record the callback behavior and test evidence in `PP-7`. Slice 08
may present these states, but must not move save decisions into the window.
