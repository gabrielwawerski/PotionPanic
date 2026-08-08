# Docs Map

`Docs/` stores current project truth for Potion Panic and hosts the shared
VitePress task board.

Published read-only site:

- `https://gabrielwawerski.github.io/PotionPanic/`
- Local editing still happens through the repo-backed VitePress dev server.

Use the board for time-bound planning, task execution, milestone notes, and
project history. Use `plans/` for active implementation plans that should stay
browsable in the VitePress site. Keep evergreen guidance in the rest of
`Docs/`.

[VitePress Project Management Feature Proposals](collaboration/vitepress-project-management-feature-proposals.md)

## Read This First

If you are new to the repo, read these in order:

1. [`onboarding/getting-started.md`](onboarding/getting-started.md)
2. [`collaboration/team-workflow.md`](collaboration/team-workflow.md)
3. [`board.md`](board.md)
4. [`project/game-design.md`](project/game-design.md)
5. [`project/mvp-scope.md`](project/mvp-scope.md)
6. [`project/technical-architecture.md`](project/technical-architecture.md)

## Folder Map

### `onboarding/`

- [`getting-started.md`](onboarding/getting-started.md)
  - first-machine setup
  - first smoke test
  - docs board bootstrap
  - coordination-sensitive files

### `collaboration/`

- [`team-workflow.md`](collaboration/team-workflow.md)
  - day-to-day team process
  - git and review workflow
  - shared Unity file rules
  - documentation ownership

### `project/`

- [`game-design.md`](project/game-design.md)
  - player-facing design
  - gameplay loop
  - ingredients, potions, and disasters
  - art and audio direction

- [`mvp-scope.md`](project/mvp-scope.md)
  - locked MVP decisions
  - run structure
  - milestone sequence
  - out-of-scope rules
  - definition of done

- [`technical-architecture.md`](project/technical-architecture.md)
  - runtime system boundaries
  - data ownership
  - component responsibilities
  - repo-aligned script structure

- [`game-design-and-psychology.md`](project/game-design-and-psychology.md)
  - research and reference guide, not locked project requirements
  - player motivation, attention, and engagement
  - game feel, feedback, and ethical retention
  - small-team design and market considerations
  - prototype and playtest validation
  - temporarily stored under `project/` until moved to a research section

### `guides/unity/`

- [`index.md`](guides/unity/index.md)
  - guide selection help

- [`runtime-architecture.md`](guides/unity/runtime-architecture.md)
  - Unity mental models
  - composition and state ownership
  - scene and system boundaries

- [`coding-and-implementation.md`](guides/unity/coding-and-implementation.md)
  - implementation habits
  - smallest-playable-loop thinking
  - failure checks and debugging

- [`editor-safety.md`](guides/unity/editor-safety.md)
  - scene, prefab, and inspector safety
  - coordination rules for Unity-side edits

- [`presentation-workflows.md`](guides/unity/presentation-workflows.md)
  - UI, animation, model, material, and feedback workflow

### `guides/`

- [`coordinated-leasing.md`](guides/coordinated-leasing.md)
  - Unity Coordination window tutorial
  - Cloudflare Worker mental model
  - developer-token setup
  - outage and operator workflows

### Task And Planning Areas

- [`board.md`](board.md)
  - shared kanban board
  - editable from the local docs server

- [`plans/`](plans/index.md)
  - active implementation plans
  - browsable in VitePress while work is in progress
  - move finished or superseded plans into the archive flow

- `tickets/`
  - one markdown file per active task

- [`milestones/`](milestones/index.md)
  - milestone overview pages

- [`archive/`](archive/index.md)
  - completed or superseded planning history

## Source Of Truth

Use one primary doc per topic:

| Topic | Canonical doc |
| --- | --- |
| Local onboarding | [`onboarding/getting-started.md`](onboarding/getting-started.md) |
| Team process | [`collaboration/team-workflow.md`](collaboration/team-workflow.md) |
| Player-facing design | [`project/game-design.md`](project/game-design.md) |
| MVP scope and milestone order | [`project/mvp-scope.md`](project/mvp-scope.md) |
| Runtime architecture | [`project/technical-architecture.md`](project/technical-architecture.md) |
| Unity implementation guidance | [`guides/unity/index.md`](guides/unity/index.md) and its linked guide pages |
| Coordinated leasing usage | [`guides/coordinated-leasing.md`](guides/coordinated-leasing.md) |
| Tasks and active plan history | [`board.md`](board.md), [`plans/index.md`](plans/index.md), `tickets/`, [`milestones/index.md`](milestones/index.md), [`archive/index.md`](archive/index.md) |

Research and reference guides are advisory. In particular,
[`project/game-design-and-psychology.md`](project/game-design-and-psychology.md)
does not override the canonical game design, MVP scope, or technical
architecture documents.

## What Does Not Belong In Evergreen Docs

Keep these out of the evergreen guidance unless the team intentionally promotes
them into reusable documentation:

- completed milestone implementation plans
- superseded active implementation plans
- one-off readiness reviews
- stale decision logs
- superseded onboarding drafts
- task-by-task execution history

Those belong in task tickets, milestone pages, or the archive.

## Update Rules

Update the relevant evergreen doc when:

- setup instructions change
- the current MVP decisions change
- the runtime architecture changes
- team workflow or review rules change
- a Unity working rule becomes stable enough to reuse

If a decision only matters for one task or one milestone execution pass, keep
it in the board workflow instead of creating a new top-level evergreen doc.
