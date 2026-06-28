# PotionPanic

Potion Panic is a small-scope Unity project for a two-person beginner team. The
target is a finished small game, not a large experimental prototype.

This repository uses a VitePress site rooted in `Docs/` as shared project state
for planning, execution, and evergreen documentation. The default task workflow
is the browser board served locally from the repo.

## Start Here

If this is your first day in the repo, read these in order:

1. `Docs/index.md`
2. `Docs/onboarding/getting-started.md`
3. `Docs/collaboration/team-workflow.md`
4. `Docs/board.md`
5. `Docs/project/game-design.md`
6. `Docs/project/mvp-scope.md`
7. `Docs/project/technical-architecture.md`

## First Day Setup

Install these once on each machine:

- Unity Hub
- Unity Editor `6000.5.1f1`
- JetBrains Rider
- Git
- Git LFS
- Node.js and npm for the docs board

Then:

1. Clone the repository.
2. Run `git lfs install`.
3. Open the repo root in Unity Hub with editor `6000.5.1f1`.
4. Let Unity finish package import and script compilation.
5. Open `PotionPanic.sln` in Rider.
6. Open `Assets/Scenes/SampleScene.unity`.
7. Press Play and confirm the project enters Play Mode without new console errors.
8. Run `.\Scripts\setup-docs.ps1`.
9. Open the docs board with `.\Scripts\docs-ui.ps1`.

Current repo note:

- The shared prototype scene is currently `Assets/Scenes/SampleScene.unity`.
- `Assets/Scenes/testscene.unity` is not the shared milestone scene unless a
  task explicitly says so.
- Milestone 1 is expected to rename or replace the shared scene as
  `Laboratory.unity`.

## Collaboration Docs

- `Docs/index.md` is the docs index and source-of-truth map.
- `Docs/board.md` is the shared task board entry point.
- `Docs/onboarding/getting-started.md` is the step-by-step onboarding guide.
- `Docs/collaboration/team-workflow.md` is the day-to-day collaboration guide.
- `Docs/project/game-design.md` is the player-facing design source of truth.
- `Docs/project/mvp-scope.md` is the MVP scope, milestone, and tuning source of
  truth.
- `Docs/project/technical-architecture.md` is the runtime structure source of
  truth.

## Docs Board Setup

Use these bootstrap commands if you need to install or verify the shared docs
tooling:

```powershell
.\Scripts\setup-docs.ps1
.\Scripts\docs-ui.ps1
```

Equivalent npm commands:

```powershell
npm run docs:dev
npm run docs:build
```

Repository rules:

- PowerShell is the primary shell expected for local project commands.
- The browser board is the default human task workflow for this repo.
- Persistent webpage editing happens through the local VitePress server, not the
  static build.
- `npm run docs:ticket` is available for CLI ticket creation when needed, but
  the board is still the main workflow.
- AI collaborators should follow the committed `AGENTS.md`, `CLAUDE.md`, and
  `GEMINI.md` instructions.

### Shared helper scripts

- `.\Scripts\setup-docs.ps1` installs or verifies the Node dependencies needed
  for the VitePress docs workflow.
- `.\Scripts\docs-ui.ps1` starts the local docs server if needed and opens the
  editable task board in the default browser.
- `.\Scripts\backlog\setup-backlog.ps1` and `.\Scripts\backlog\backlog-ui.ps1`
  remain as temporary compatibility wrappers that redirect to the new docs
  flow.
