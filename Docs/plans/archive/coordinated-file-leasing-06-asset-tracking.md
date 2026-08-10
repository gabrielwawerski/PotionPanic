---
title: 'Coordinated Leasing 06: Scene and Selected-Prefab Tracking'
---

# Coordinated Leasing 06: Scene and Selected-Prefab Tracking

**Session goal:** Publish presence and request leases from actual Unity editor scene and Prefab Stage lifecycle events.

**Depends on:** Slices 01, 05A, and 05B. Slice 06 remains paused until both stabilization gates are green.

**Produces:** A tracker that emits one coherent coordination event per loaded scene or selected prefab transition and survives reconnect and domain reload.

## Files

- Create tracker files under `Assets/Scripts/Editor/Coordination/` for scene callbacks, Prefab Stage callbacks, coordinated-rule evaluation, and tracker state.
- Add matching tests under `Assets/Tests/EditMode/Coordination/`.

## Implementation steps

- Subscribe and unsubscribe cleanly from installed Unity scene and Prefab Stage open, dirty, save, and close callbacks.
- Track untitled scenes, additive scenes, duplicate callbacks, domain reload, selected prefabs, and non-coordinated prefabs without publishing invalid paths.
- Publish presence only for enabled rules. Acquire editing leases only for
  `exclusive` rules.
- On scene or Prefab Stage close, release presence and the owned editing lease. The server-side reservation resurfaces automatically.
- After reconnect or domain reload, inventory loaded stages, republish presence, and reacquire leases for stages that are already dirty.
- Add a local authoritative state store used by the save guard.
- Do not invent prefab paths. The current allowlist remains empty until real prefabs exist.
- Pass Git branch and task context through the Slice 05 connection service; the tracker does not read or write machine-local settings and never stores context in tracked files.

## Verification

Run the Coordination EditMode suite with focused coverage for untitled and additive scenes, duplicate callbacks, domain reload, selected and excluded prefabs, reconnect republish, close release, and reservation conversion. Inspect the Unity Console after assembly reload and confirm no duplicate subscriptions.

**Commit:** `feat(coordination): track coordinated scenes and prefabs`

**Handoff:** Record the tracker event contract, coordinated path allowlist, and test evidence in `PP-7`. Slice 07 may consume tracker state and lease results; it must not call Unity save APIs from tracker callbacks.
