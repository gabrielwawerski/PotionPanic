# Potion Panic Milestone 1 Implementation Plan

## Summary

Implement only the first milestone from the current Unity scaffold: a fixed orthographic top-down camera, responsive WASD movement, player collision, and a basic one-room laboratory blockout.

This milestone should convert the current placeholder scene into the canonical gameplay scene immediately, so all later milestones build on `Laboratory.unity` instead of `SampleScene.unity`.

## Key Changes

- Scene and camera
  - Rename `Assets/Scenes/SampleScene.unity` to `Assets/Scenes/Laboratory.unity`.
  - Update both build settings and the Unity template default scene reference to point to `Laboratory.unity`.
  - Rework the scene into a centered `16 x 16` lab blockout: one floor, four perimeter walls, player spawn at room center.
  - Keep the existing `Main Camera`, but change it to orthographic, position it at `(0, 18, 0)`, rotate it to `(90, 0, 0)`, and set orthographic size to `10`.
  - Keep the camera fully static: no follow, zoom, tilt animation, or perspective fallback.

- Player movement and collision
  - Add a runtime `PlayerController` MonoBehaviour under the gameplay assembly.
  - Use `CharacterController`, not `Rigidbody`, for Milestone 1 movement and collision.
  - Move only on world X/Z axes; no camera-relative movement, no jumping, no sprint, no gravity gameplay, and no forced facing or rotation behavior in this milestone.
  - Use a placeholder player capsule with `CharacterController` height `1.8`, radius `0.4`, center `(0, 0.9, 0)`, spawned at `(0, 0, 0)`.
  - Use a default move speed of `6` world units per second.
  - Normalize diagonal input so diagonal speed matches straight-line speed.

- Input wiring
  - Reuse the existing `Assets/InputSystem_Actions.inputactions` asset.
  - Read only the existing `Player/Move` action in Milestone 1.
  - Wire movement through a serialized `InputActionReference` on `PlayerController`; do not introduce `PlayerInput`, interaction handling, or input-asset cleanup yet.
  - Leave `Look`, `Interact`, `Attack`, `Sprint`, and other actions untouched for later milestones.

- Blockout and collision setup
  - Use primitive geometry only: one floor plane or cube and four wall cubes with colliders.
  - Use perimeter wall thickness `0.5` and height `1.5`.
  - Ensure the player cannot leave the room or pass through walls.
  - Do not add interactables, brewing stations, UI, disasters, or managers in this milestone.

## Important Types

- `PlayerController`
  - Serialized fields: `CharacterController characterController`, `InputActionReference moveAction`, `float moveSpeed`
  - Responsibility: read `Move`, convert it to normalized world-space X/Z movement, and move the `CharacterController` frame-independently

- Optional pure helper
  - Add a small pure movement-math helper only if needed to support deterministic EditMode testing of axis conversion and diagonal normalization

## Test Plan

- Automated
  - Add one EditMode test covering world-axis movement conversion and diagonal normalization.
  - Add one PlayMode test that loads `Laboratory.unity` and verifies:
    - the scene contains a main camera
    - the camera is orthographic
    - the player object with `CharacterController` exists

- Manual acceptance
  - Open `Laboratory.unity` and confirm the full room is readable with the static orthographic camera.
  - Move with WASD to all four walls.
  - Confirm straight and diagonal movement feel equally fast.
  - Confirm the player cannot leave the room.
  - Confirm no mouse-look, follow-camera behavior, or unintended physics drift is present.

## Assumptions

- The camera decision is locked as orthographic top-down.
- The movement implementation decision is locked as `CharacterController`.
- Milestone 1 should adopt the canonical `Laboratory.unity` scene now rather than deferring the rename.
- Existing input asset reuse is preferred over input cleanup at this stage.
- Interaction, inventory, brewing, disaster logic, and run-flow UI remain out of scope until later milestones.
