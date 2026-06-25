---
id: PP-3
title: Add Milestone 1 CharacterController movement
status: To Do
assignee: []
created_date: '2026-06-25 00:24'
updated_date: '2026-06-25 00:29'
labels: []
milestone: m-0
dependencies:
  - PP-2
documentation:
  - Docs/plans/milestone-1-implementation-plan.md
  - Docs/Potion Panic.md
  - Docs/Potion Panic - Technical Architecture.md
modified_files:
  - Assets/Scripts/Runtime
  - Assets/InputSystem_Actions.inputactions
  - Assets/Scenes/Laboratory.unity
priority: medium
ordinal: 11000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Add the first gameplay movement implementation for Milestone 1 using the existing input asset and a world-aligned CharacterController-based player controller.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 PlayerController in the gameplay assembly reads only Player/Move from the existing input asset through InputActionReference.
- [ ] #2 Movement is world-aligned on X/Z, uses normalized diagonal input, and exposes move speed.
- [ ] #3 The player uses the locked Milestone 1 CharacterController settings and cannot clip through room bounds.
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Created as planning/kanban data only. No implementation started.
<!-- SECTION:NOTES:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [ ] #1 Tests pass
- [ ] #2 Documentation updated
- [ ] #3 No regressions introduced
<!-- DOD:END -->
