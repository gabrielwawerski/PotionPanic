---
id: 3
title: Add Milestone 1 CharacterController movement
status: backlog
priority: medium
milestone: m-0
dependencies:
  - PP-2
documentation:
  - README.md
  - project/mvp-scope.md
  - project/technical-architecture.md
  - project/game-design.md
  - onboarding/getting-started.md
  - collaboration/team-workflow.md
affectedFiles:
  - Assets/Scripts/Runtime
  - Assets/Tests/EditMode
  - Assets/Scenes/Laboratory.unity
tags: []
order: 2
---

## Description

Add the Milestone 1 movement foundation in the gameplay assembly using the existing
`Player/Move` input action through `InputActionReference`, plus the minimal player setup inside `Laboratory.unity`. Keep the scope to world-aligned movement only: no look, sprint, jump, gravity gameplay, camera-relative movement, interaction, or input-asset redesign.

## Acceptance Criteria

- [ ] #1 `PlayerController` in the gameplay assembly reads only `Player/Move` from the existing input asset through `InputActionReference`.
- [ ] #2 Movement is world-aligned on X/Z, keeps diagonal speed normalized, exposes move speed, and does not add look, sprint, jump, or camera-relative behavior.
- [ ] #3 `Laboratory.unity` contains a minimal player setup with a `CharacterController`, and the Milestone 1 controller configuration is recorded in the task notes (`radius`, `height`, `center`, `slopeLimit`, `stepOffset`, `skinWidth`, `minMoveDistance`).
- [ ] #4 EditMode coverage exists for axis conversion and diagonal normalization.
- [ ] #5 The player cannot clip through the room bounds during the Milestone 1 smoke test.

## Implementation Plan

- [ ] Review the `Laboratory` scene handoff notes and confirm the existing input asset already exposes the `Player/Move` action needed for Milestone 1.
- [ ] Add EditMode coverage for axis conversion and diagonal normalization before gameplay implementation so the intended movement math is locked first.
- [ ] Implement `PlayerController` in the gameplay assembly using `InputActionReference` to read `Player/Move` and convert `Vector2` input into world-aligned X/Z motion with normalized diagonals and serialized move speed.
- [ ] Add the minimal player setup to `Laboratory.unity` with a `CharacterController`, wire it to the existing input asset, and record the final Milestone 1 controller values in the task notes.
- [ ] Run the targeted EditMode verification plus manual Play Mode movement checks against the room bounds, then capture any handoff details needed by final milestone validation.

## Implementation Notes

Created as planning and kanban data only. No implementation started.

2026-06-28 backlog refinement: PP-3 now owns movement-specific EditMode coverage and the minimal player setup in `Laboratory`. Record the final `CharacterController` values here during implementation: `radius`, `height`, `center`, `slopeLimit`, `stepOffset`,
`skinWidth`, `minMoveDistance`.

## Definition of Done

- [ ] #1 Acceptance criteria met
- [ ] #2 Relevant Unity verification completed
- [ ] #3 No new relevant Console errors
- [ ] #4 Documentation or task notes updated when needed
- [ ] #5 Branch committed and ready for review or merge

## Notes

