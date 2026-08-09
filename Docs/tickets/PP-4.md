---
id: 4
title: Validate Laboratory milestone and align scene-name docs
status: backlog
priority: medium
milestone: m-0
dependencies:
  - PP-2
  - PP-3
documentation:
  - README.md
  - project/mvp-scope.md
  - onboarding/getting-started.md
  - collaboration/team-workflow.md
  - AGENTS.md
  - CLAUDE.md
  - GEMINI.md
affectedFiles:
  - Assets/Tests/PlayMode
  - README.md
  - onboarding/getting-started.md
  - collaboration/team-workflow.md
  - project/mvp-scope.md
  - AGENTS.md
  - CLAUDE.md
  - GEMINI.md
tags: []
order: 3
---

## Description

Finish Milestone 1 by adding the final PlayMode validation for `Laboratory.unity`, confirming the manual movement smoke test, and aligning collaborator docs and repo instructions so `Laboratory` becomes the canonical shared gameplay scene after PP-2 and PP-3 land.

## Acceptance Criteria

- [ ] #1 PlayMode coverage loads `Laboratory.unity` and verifies the main camera is orthographic.
- [ ] #2 PlayMode coverage verifies the Milestone 1 player setup in `Laboratory.unity`
  includes a `CharacterController`.
- [ ] #3 Current collaborator docs and repo instruction files no longer tell contributors to open or smoke-test `SampleScene.unity` as the shared milestone scene once Milestone 1 is complete.
- [ ] #4 Manual smoke verification confirms WASD movement reaches all four walls and the player cannot leave the room.

## Implementation Plan

- [ ] Review the final `Laboratory` scene and movement setup from PP-2 and PP-3, plus every current collaborator-facing instruction that still mentions `SampleScene` as the shared gameplay scene.
- [ ] Add PlayMode coverage that loads `Laboratory.unity` and verifies the orthographic main camera plus the Milestone 1 player `CharacterController` setup.
- [ ] Update the README, onboarding docs, collaboration workflow, MVP scope notes, and repo instruction files so `Laboratory.unity` is the canonical shared gameplay scene while preserving the `testscene` caveat if it still applies.
- [ ] Run the manual Milestone 1 smoke test, then record any remaining handoff or follow-up notes before marking the milestone ready for completion.

## Implementation Notes

Created as planning and kanban data only. No implementation started.

2026-06-28 backlog refinement: PP-4 is the Milestone 1 exit-gate task. It owns final PlayMode validation plus the post-rename docs and instruction sweep, not movement math or scene-conversion implementation.

## Definition of Done

- [ ] #1 Acceptance criteria met
- [ ] #2 Relevant Unity verification completed
- [ ] #3 No new relevant Console errors
- [ ] #4 Documentation or task notes updated when needed
- [ ] #5 Branch committed and ready for review or merge

## Notes

