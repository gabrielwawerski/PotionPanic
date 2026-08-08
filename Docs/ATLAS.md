# Documentation Atlas

Use this map to find the current owner for project knowledge. It is a routing
map, not a complete inventory.

## Entry Points

- `README.md`: repository overview, first commands, and public docs operations.
- `AGENTS.md`: always-read contributor and agent rules.
- [`index.md`](index.md): published docs home.
- [`board.md`](board.md): editable task board for active work.

## Setup And Workflow

- [`onboarding/getting-started.md`](onboarding/getting-started.md): Project
  Setup for a new machine, including the board, Unity, Rider, smoke test, and
  Coordination login. Open this before taking a first task.
- [`collaboration/team-workflow.md`](collaboration/team-workflow.md): Daily
  Workflow for choosing, executing, testing, reviewing, and handing off work.
  Open this after Project Setup and before a normal work session.

## Project Truth

- [`project/index.md`](project/index.md): Project Overview and reading order
  for binding game, scope, and runtime decisions.
- [`project/game-design.md`](project/game-design.md): player-facing design,
  core loop, content themes, disasters, scoring intent, art, and audio.
- [`project/mvp-scope.md`](project/mvp-scope.md): locked MVP decisions,
  milestone sequence, hard scope boundaries, and definition of done.
- [`project/technical-architecture.md`](project/technical-architecture.md):
  Potion Panic Runtime Contract for data assets, component responsibilities,
  dependencies, and completion criteria.

## Unity Working Guides

- [`guides/index.md`](guides/index.md): task-based guide directory. Open this
  when you know what kind of work you are doing but not which guide applies.
- [`unity-guides/index.md`](unity-guides/index.md): which Unity guide to open.
- [`unity-guides/runtime-architecture.md`](unity-guides/runtime-architecture.md):
  Unity mental model, composition, state ownership, and scene boundaries.
- [`unity-guides/coding-and-implementation.md`](unity-guides/coding-and-implementation.md):
  implementation habits, small playable slices, debugging, and checks.
- [`unity-guides/editor-safety.md`](unity-guides/editor-safety.md): scene,
  prefab, ProjectSettings, package, and inspector-change safety.
- [`unity-guides/presentation-workflows.md`](unity-guides/presentation-workflows.md):
  UI, animation, materials, VFX, and feedback workflows.

## Coordination

- [`guides/coordinated-leasing.md`](guides/coordinated-leasing.md): developer
  tutorial for the Unity Coordination window, its actions and claim states,
  save conflicts, and the manual fallback. Open this before editing a
  coordinated scene or when the window needs explanation.
- [Coordination Server README](https://github.com/gabrielwawerski/PotionPanic/blob/master/Tools/CoordinationServer/README.md): operator-only Worker verification,
  local development, manual deployment, token operations, monitoring, outage
  handling, and secret rotation.
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
  advisory research on engagement, ethics, game feel, market presentation,
  and validation. It does not override project design, scope, or architecture.

## Update Rules

- Update the owning evergreen doc when setup, workflow, architecture, design,
  MVP scope, or reusable Unity guidance changes.
- Keep task-by-task execution notes in tickets.
- Keep current implementation plans in `plans/`.
- Move completed or superseded plans to `plans/archive/`.
- Preserve historical archive prose unless a link, route, or frontmatter value
  is wrong for the current file path.
