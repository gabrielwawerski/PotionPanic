---
title: 'Coordinated Leasing 06: Scene and Selected-Prefab Tracking'
---

# Coordinated Leasing 06: Scene and Selected-Prefab Tracking

**Session goal:** Publish presence and request leases from actual Unity editor
scene and Prefab Stage lifecycle events.

**Depends on:** Slices 01 and 05.

**Produces:** A tracker that emits one coherent coordination event per loaded
scene or selected prefab transition and survives reconnect and domain reload.

## Files

- Create tracker files under `Assets/Scripts/Editor/Coordination/` for scene
  callbacks, Prefab Stage callbacks, coordinated-rule evaluation, and tracker
  state.
- Add matching tests under `Assets/Tests/EditMode/Coordination/`.

## Implementation steps

- Subscribe and unsubscribe cleanly from installed Unity scene and Prefab Stage
  open, dirty, save, and close callbacks.
- Track untitled scenes, additive scenes, duplicate callbacks, domain reload,
  selected prefabs, and non-coordinated prefabs without publishing invalid
  paths.
- Publish `viewing` only for enabled rules. Request exactly one editing lease
  on the first meaningful dirty transition. Convert the developer's reservation
  to editing at that transition rather than issuing two claims.
- Republish loaded stages after reconnect or domain reload. Release presence on
  close where the editor callback allows it; leave abrupt cleanup to server
  expiry.
- Keep Git branch and task context as data supplied to the connection service;
  do not read or write machine-local context into tracked files.

## Verification

Run the Coordination EditMode suite with focused coverage for untitled and
additive scenes, duplicate callbacks, domain reload, selected and excluded
prefabs, reconnect republish, close release, and reservation conversion. Inspect
the Unity Console after assembly reload and confirm no duplicate subscriptions.

**Commit:** `feat(coordination): track coordinated scenes and prefabs`

**Handoff:** Record the tracker event contract, coordinated path allowlist, and
test evidence in `PP-7`. Slice 07 may consume tracker state and lease results;
it must not call Unity save APIs from tracker callbacks.
