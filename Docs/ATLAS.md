# Documentation Atlas

Use this map when you know the question but not which document owns the answer. For the required content and authority of each evergreen page, use the
[Evergreen Documentation Contract](evergreen-documentation.md).

## Entry points

- [Repository README](https://github.com/gabrielwawerski/PotionPanic/blob/master/README.md): repository purpose, first commands, local versus published docs, and maintainer operations. Open it when entering the repository.
- [`index.md`](index.md): published docs home. Open it when browsing the site.
- [`board.md`](board.md): active task board. Open it to choose or update work.
- [`evergreen-documentation.md`](evergreen-documentation.md): knowledge owners, writing model, and evidence rules. Open it before changing long-lived docs.
- [AGENTS.md](https://github.com/gabrielwawerski/PotionPanic/blob/master/AGENTS.md): always-read contributor and agent constraints.

## Setup and recurring work

- [`onboarding/getting-started.md`](onboarding/getting-started.md): complete first-machine setup for Git, Docboard, Unity, Rider or VS Code, the smoke test, and a Coordination identity.
- [`collaboration/team-workflow.md`](collaboration/team-workflow.md): move a normal task through the GUI-first JetBrains or VS Code Git workflow, shared asset coordination, implementation, verification, handoff, review, and local merge.

## Project truth

- [`project/index.md`](project/index.md): choose among the binding project documents.
- [`project/game-design.md`](project/game-design.md): player experience, core loop, content intent, pressure, reward, art, and audio.
- [`project/mvp-scope.md`](project/mvp-scope.md): accepted MVP behavior, tuning, milestone dependencies, scope boundaries, and game-level completion.
- [`project/technical-architecture.md`](project/technical-architecture.md):
  accepted target runtime data, responsibilities, ownership, and flow.

## Working guides

- [`guides/index.md`](guides/index.md): choose practical guidance by task.
- [`guides/unity/index.md`](guides/unity/index.md): choose a Unity working guide.
- [`guides/unity/runtime-architecture.md`](guides/unity/runtime-architecture.md):
  understand Unity runtime composition, lifecycle, dependencies, and state.
- [`guides/unity/coding-and-implementation.md`](guides/unity/coding-and-implementation.md):
  turn one accepted behavior into a small, verifiable implementation slice.
- [`guides/unity/editor-safety.md`](guides/unity/editor-safety.md): change scenes, prefabs, serialized references, shared assets, and settings safely.
- [`guides/unity/presentation-workflows.md`](guides/unity/presentation-workflows.md):
  build and hand off UI, animation, models, materials, VFX, and feedback.

## Coordination

- [`guides/coordinated-leasing.md`](guides/coordinated-leasing.md): developer mental model and workflow for the Unity Coordination tool, claims, automatic lifecycle, save conflicts, credentials, and troubleshooting.
- [Coordination Server README](https://github.com/gabrielwawerski/PotionPanic/blob/master/Tools/CoordinationServer/README.md):
  operator trust model, verification, local development, deployment, token administration, monitoring, and protocol lifecycle.
- [`plans/coordinated-file-leasing-system.md`](plans/coordinated-file-leasing-system.md):
  Protocol v1 and program contract.
- [`plans/coordinated-file-leasing-release-acceptance.md`](plans/coordinated-file-leasing-release-acceptance.md):
  remaining release-acceptance work.
- [`tickets/PP-7.md`](tickets/PP-7.md): dated evidence and current blockers.

## Active work and history

- [`plans/index.md`](plans/index.md): active long-form implementation plans.
- [`chronicles/`](chronicles/): implementation decisions and verification records for work that needs a durable execution narrative.
- `tickets/`: one Markdown file per active task; use the board for the task UI.
- [`plans/archive/index.md`](plans/archive/index.md): completed or superseded plans.
- [`archive/index.md`](archive/index.md): archived board and task records.

## Research

- [`research/game-design-and-psychology.md`](research/game-design-and-psychology.md):
  advisory evidence and heuristics for engagement, ethics, feedback, production risk, market communication, and validation. It does not override project truth.

## Update rules

- Update the evergreen owner when a stable fact or accepted decision changes.
- Keep execution details in tickets, plans, and chronicles.
- Archive completed or superseded plans only through the established plan lifecycle.
- Preserve historical prose. Correct links and active file references when the documentation structure moves.
