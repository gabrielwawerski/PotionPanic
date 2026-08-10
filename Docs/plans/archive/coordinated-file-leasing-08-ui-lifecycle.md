---
title: 'Coordinated Leasing 08: Coordination Window and Lifecycle'
---

# Coordinated Leasing 08: Coordination Window and Lifecycle

**Session goal:** Make coordination state inspectable and operable in the Unity editor, and make normal shutdown, compilation, and domain reload safe.

**Depends on:** Slices 05B, 06, and 07.

**Produces:** The first usable editor interface and lifecycle-hardening layer.

## Files

- Create the Coordination window, view-model, notifications, and lifecycle bootstrap files under `Assets/Scripts/Editor/Coordination/`.
- Add matching tests under `Assets/Tests/EditMode/Coordination/`.

## Implementation steps

- Add `Window > Potion Panic > Coordination` showing authenticated identity, the Git-derived branch, editable local task context, connection state, presence, leases, reservations, owner details, expiry, and the local
  `Disabled` control. Persist only the task context and disabled switch through Slice 01's untracked local settings store.
- Add actions to reconnect, reserve, release, override, copy a canonical path, and forget credentials. Disable actions that cannot be safely sent offline.
- Use Unity notifications only for claims, conflicts, overrides, reservations, authentication failure, and prolonged disconnect. Do not add native Windows notifications or Rider integration.
- Own service startup and `ShutdownAsync`, including cancellation of heartbeat and reconnect loops before socket shutdown.
- Prevent duplicate bootstrap, heartbeat, reconnect, and event subscriptions across domain reload.
- Display uncoordinated-save warnings until the affected asset closes or coordination confirms ownership.
- Release owned presence and editing leases during normal shutdown where possible. Treat abrupt shutdown as stale expiry.

## Verification

Run Coordination EditMode tests for view-model state, action enablement, notification filtering, normal shutdown release, domain reload, duplicate bootstrap prevention, unsupported-platform disabling, task-context persistence, and disabled-switch persistence. Open the window in Unity, confirm the menu item and key states render, then run the coordinated scene in Play Mode and review the Console for new errors.

**Commit:** `feat(coordination): add editor coordination interface`

**Handoff:** Record UI and lifecycle evidence in `PP-7`. Slice 09 is the only remaining session and may deploy, use real credentials, and update evergreen documentation.
