# Documentation Atlas

Use this map to find the current owner for project knowledge. It is a routing
map, not a complete file inventory.

## Entry Points

- `README.md`: repository overview, first commands, and public docs operations.
- `AGENTS.md`: always-read contributor and agent rules.
- [`index.md`](index.md): published docs home.
- [`board.md`](board.md): editable task board for active work.

## Setup And Workflow

- [`onboarding/getting-started.md`](onboarding/getting-started.md): first
  machine setup, Unity smoke test, docs board startup, and collaboration safety.
- [`collaboration/team-workflow.md`](collaboration/team-workflow.md): daily
  workflow, Git rules, Unity protected-file coordination, testing, and
  documentation ownership.

## Project Truth

- [`project/game-design.md`](project/game-design.md): player-facing design,
  core loop, content themes, disasters, scoring intent, art, and audio.
- [`project/mvp-scope.md`](project/mvp-scope.md): locked MVP decisions,
  milestone sequence, hard scope boundaries, and definition of done.
- [`project/technical-architecture.md`](project/technical-architecture.md):
  runtime ownership, data assets, component responsibilities, and dependency
  rules.
- [`milestones/index.md`](milestones/index.md): milestone pages that are still
  active or worth browsing from the docs site.

## Unity Working Guides

- [`guides/unity/index.md`](guides/unity/index.md): which Unity guide to open.
- [`guides/unity/runtime-architecture.md`](guides/unity/runtime-architecture.md):
  Unity mental model, composition, state ownership, and scene boundaries.
- [`guides/unity/coding-and-implementation.md`](guides/unity/coding-and-implementation.md):
  implementation habits, small playable slices, debugging, and checks.
- [`guides/unity/editor-safety.md`](guides/unity/editor-safety.md): scene,
  prefab, ProjectSettings, package, and inspector-change safety.
- [`guides/unity/presentation-workflows.md`](guides/unity/presentation-workflows.md):
  UI, animation, materials, VFX, and feedback workflows.

## Coordination

- [`guides/coordinated-leasing.md`](guides/coordinated-leasing.md): how to use
  the Unity Coordination window, read lease states, handle outages, and perform
  operator tasks.
- `Tools/CoordinationServer/README.md`: Worker verification, local development,
  manual deployment, token operations, monitoring, outage handling, and secret
  rotation.
- [`plans/coordinated-file-leasing-system.md`](plans/coordinated-file-leasing-system.md):
  Protocol v1 and durable coordination program contract.
- [`plans/coordinated-file-leasing-release-acceptance.md`](plans/coordinated-file-leasing-release-acceptance.md):
  remaining PP-7 acceptance work after deployment and local implementation.
- [`tickets/PP-7.md`](tickets/PP-7.md): dated coordination evidence and open
  release blockers.

## Active Work And History

- [`plans/index.md`](plans/index.md): active implementation plans. Move only
  unfinished or current plan pages here.
- [`plans/archive/index.md`](plans/archive/index.md): completed or superseded
  implementation plans. This is the only plan archive.
- `tickets/`: one Markdown file per active task. Use [`board.md`](board.md) for
  the active task UI.
- [`archive/tickets/index.md`](archive/tickets/index.md): archived task
  records.
- [`archive/board.md`](archive/board.md): archived board view.
- [`archive/index.md`](archive/index.md): archive entry point.

## Research

- [`research/game-design-and-psychology.md`](research/game-design-and-psychology.md):
  advisory research on player motivation, game feel, engagement, market risk,
  and playtest framing. It does not override the project design, scope, or
  architecture docs.

## Update Rules

- Update the owning evergreen doc when setup, workflow, architecture, design,
  MVP scope, or reusable Unity guidance changes.
- Keep task-by-task execution notes in tickets.
- Keep current implementation plans in `plans/`.
- Move completed or superseded plans to `plans/archive/`.
- Preserve historical archive prose unless a link, route, or frontmatter value
  is wrong for the current file path.
