# Docs Index

`Docs/` stores current project truth for Potion Panic.

Use Backlog for time-bound planning, task execution, milestone notes, and
project history. Do not park superseded implementation plans in this folder.

## Read This First

If you are new to the repo, read these in order:

1. [`onboarding/getting-started.md`](onboarding/getting-started.md)
2. [`collaboration/team-workflow.md`](collaboration/team-workflow.md)
3. [`project/game-design.md`](project/game-design.md)
4. [`project/mvp-scope.md`](project/mvp-scope.md)
5. [`project/technical-architecture.md`](project/technical-architecture.md)

## Folder Map

### `onboarding/`

- `getting-started.md`
  - first-machine setup
  - first smoke test
  - Backlog tooling bootstrap
  - coordination-sensitive files

### `collaboration/`

- `team-workflow.md`
  - day-to-day team process
  - git and review workflow
  - shared Unity file rules
  - documentation ownership

### `project/`

- `game-design.md`
  - player-facing design
  - gameplay loop
  - ingredients, potions, and disasters
  - art and audio direction

- `mvp-scope.md`
  - locked MVP decisions
  - run structure
  - milestone sequence
  - out-of-scope rules
  - definition of done

- `technical-architecture.md`
  - runtime system boundaries
  - data ownership
  - component responsibilities
  - repo-aligned script structure

### `guides/unity/`

- `README.md`
  - guide selection help

- `runtime-architecture.md`
  - Unity mental models
  - composition and state ownership
  - scene and system boundaries

- `coding-and-implementation.md`
  - implementation habits
  - smallest-playable-loop thinking
  - failure checks and debugging

- `editor-safety.md`
  - scene, prefab, and inspector safety
  - coordination rules for Unity-side edits

- `presentation-workflows.md`
  - UI, animation, model, material, and feedback workflow

## Source of Truth

Use one primary doc per topic:

| Topic | Canonical doc |
| --- | --- |
| Local onboarding | `onboarding/getting-started.md` |
| Team process | `collaboration/team-workflow.md` |
| Player-facing design | `project/game-design.md` |
| MVP scope and milestone order | `project/mvp-scope.md` |
| Runtime architecture | `project/technical-architecture.md` |
| Unity implementation guidance | `guides/unity/*` |
| Tasks, milestone execution, and planning history | Backlog |

## What Does Not Belong Here

Keep these out of `Docs/` unless the team intentionally promotes them into
evergreen guidance:

- completed milestone implementation plans
- one-off readiness reviews
- stale decision logs
- superseded onboarding drafts
- task-by-task execution history

Those belong in Backlog tasks, documents, or decisions.

## Update Rules

Update the relevant evergreen doc when:

- setup instructions change
- the current MVP decisions change
- the runtime architecture changes
- team workflow or review rules change
- a Unity working rule becomes stable enough to reuse

If a decision only matters for one task or one milestone execution pass, store
it in Backlog instead of creating a new top-level doc.
