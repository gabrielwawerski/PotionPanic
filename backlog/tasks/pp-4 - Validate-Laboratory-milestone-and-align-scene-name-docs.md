---
id: PP-4
title: Validate Laboratory milestone and align scene-name docs
status: To do
assignee: []
created_date: '2026-06-25 00:24'
updated_date: '2026-06-25 13:39'
labels: []
milestone: m-0
dependencies:
  - PP-2
  - PP-3
documentation:
  - Docs/plans/milestone-1-implementation-plan.md
  - README.md
  - Docs/getting-started.md
  - Docs/team-workflow-guide.md
  - Docs/plans/implementation-readiness-review.md
modified_files:
  - Assets/Tests/EditMode
  - Assets/Tests/PlayMode
  - README.md
  - Docs/getting-started.md
  - Docs/team-workflow-guide.md
  - Docs/plans/implementation-readiness-review.md
priority: low
ordinal: 12000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Finish Milestone 1 by adding the scoped automated validation and updating collaborator docs so the renamed shared gameplay scene and smoke-test workflow stay accurate.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 EditMode coverage exists for axis conversion and diagonal normalization.
- [ ] #2 PlayMode coverage loads Laboratory.unity and verifies the main camera is orthographic and the player has a CharacterController.
- [ ] #3 Current collaborator docs no longer instruct people to open or smoke-test SampleScene.unity as the shared gameplay scene.
- [ ] #4 Manual smoke verification confirms WASD movement reaches all four walls and the player cannot leave the room.
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Created as planning/kanban data only. No implementation started.
<!-- SECTION:NOTES:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [ ] #1 Acceptance criteria met
- [ ] #2 Relevant Unity verification completed
- [ ] #3 No new relevant Console errors
- [ ] #4 Documentation or Backlog notes updated when needed
- [ ] #5 Branch committed and ready for review or merge
<!-- DOD:END -->
