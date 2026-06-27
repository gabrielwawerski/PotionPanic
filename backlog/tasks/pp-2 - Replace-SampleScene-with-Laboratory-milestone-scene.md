---
id: PP-2
title: Replace SampleScene with Laboratory milestone scene
status: To Do
assignee:
  - Patro
created_date: '2026-06-25 00:24'
updated_date: '2026-06-27 14:49'
labels: []
milestone: m-0
dependencies: []
documentation:
  - Docs/plans/milestone-1-implementation-plan.md
  - Docs/plans/implementation-readiness-review.md
  - Docs/Potion Panic.md
  - Docs/Potion Panic - Technical Architecture.md
modified_files:
  - Assets/Scenes/SampleScene.unity
  - Assets/Scenes/Laboratory.unity
  - ProjectSettings/EditorBuildSettings.asset
  - ProjectSettings/ProjectSettings.asset
priority: high
ordinal: 5500
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Convert the current shared placeholder scene into the canonical Milestone 1 gameplay scene so later milestones build on Laboratory instead of SampleScene.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Assets/Scenes/Laboratory.unity is the shared gameplay scene and project scene defaults no longer point at SampleScene.unity.
- [ ] #2 The scene contains a static orthographic main camera and a one-room lab blockout with floor, perimeter walls, and center spawn.
- [ ] #3 No Milestone 2 or later systems are introduced.
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. Verify current scene/build-setting/template-default references and capture the current SampleScene baseline.
2. Create a failing PlayMode test that loads the canonical Laboratory scene name and checks for an orthographic main camera.
3. Run the targeted PlayMode test to confirm it fails because Laboratory.unity does not exist yet.
4. Rename or replace SampleScene as Laboratory, then update EditorBuildSettings and templateDefaultScene to the new path.
5. Rework the scene into the Milestone 1 blockout only: static orthographic camera, centered 16x16 room, perimeter walls, and center spawn placeholder.
6. Re-run the targeted PlayMode test and inspect the scene asset/config references until it passes.
7. Append notes with any scene-asset specifics needed by the later movement and docs-validation tasks.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Board setup only. No implementation started in code or scene assets. The task plan is recorded for future execution, but the task remains unstarted pending explicit implementation work.
<!-- SECTION:NOTES:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [ ] #1 Acceptance criteria met
- [ ] #2 Relevant Unity verification completed
- [ ] #3 No new relevant Console errors
- [ ] #4 Documentation or Backlog notes updated when needed
- [ ] #5 Branch committed and ready for review or merge
<!-- DOD:END -->
