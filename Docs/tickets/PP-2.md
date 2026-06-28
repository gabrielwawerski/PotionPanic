---
id: 2
title: Replace SampleScene with Laboratory milestone scene
status: todo
priority: high
tags:
  - asd
---

## Description

Convert the current shared placeholder scene into the canonical Milestone 1 gameplay scene
by renaming or replacing `SampleScene.unity` as `Laboratory.unity`, updating shared scene
references, and delivering the fixed top-down lab blockout that later milestones build on.

## Acceptance Criteria


- [ ] #1 `Assets/Scenes/Laboratory.unity` is the shared gameplay scene, and project scene
  defaults no longer point at `SampleScene.unity`.
- [ ] #2 The scene contains a static orthographic main camera and a one-room lab blockout
  with floor, perimeter walls, and a center spawn marker.
- [ ] #3 No Milestone 2 or later systems are introduced into the scene conversion task.


## Implementation Plan


1. Verify the current shared-scene references in the scene asset, build settings, and
   project defaults, and capture any baseline details that later tasks need.
2. Rename or replace `SampleScene.unity` as `Laboratory.unity`, then update
   `EditorBuildSettings` and the project default-scene reference to the canonical
   Milestone 1 path.
3. Rework the shared scene into the Milestone 1 blockout only: static orthographic main
   camera, centered room, perimeter walls, and a center spawn marker.
4. Verify the converted scene stays within Milestone 1 scope, then record any
   scene-specific handoff notes needed by the movement and final-validation tasks.


## Implementation Notes

Board setup only. No implementation started in code or scene assets.

2026-06-28 backlog refinement: PP-2 remains the single shared-scene task. Automated
PlayMode validation was moved out of this card so scene conversion ownership stays
separate from final milestone validation.

## Definition of Done


- [ ] #1 Acceptance criteria met
- [ ] #2 Relevant Unity verification completed
- [ ] #3 No new relevant Console errors
- [ ] #4 Documentation or task notes updated when needed
- [ ] #5 Branch committed and ready for review or merge

## Notes
- Milestone: `m-0`
- Documentation: `README.md`, [`../project/mvp-scope.md`](../project/mvp-scope.md),
  [`../project/technical-architecture.md`](../project/technical-architecture.md),
  [`../project/game-design.md`](../project/game-design.md),
  [`../onboarding/getting-started.md`](../onboarding/getting-started.md),
  [`../collaboration/team-workflow.md`](../collaboration/team-workflow.md)
- Likely affected files: `Assets/Scenes/SampleScene.unity`,
  `Assets/Scenes/Laboratory.unity`, `ProjectSettings/EditorBuildSettings.asset`,
  `ProjectSettings/ProjectSettings.asset`
