# PotionPanic

Potion Panic is a small-scope Unity project for a two-person beginner team. The
target is a finished small game, not a large experimental prototype.

This repository uses a VitePress site rooted in `Docs/` as shared project state
for planning, execution, and evergreen documentation. The reusable board,
sidebar, ticket, plan, and theme tooling comes from the private
`@gabrielwawerski/docboard` package; this repo owns the docs content, board
frontmatter, and thin PotionPanic VitePress adapters.

Published docs site:

- `https://gabrielwawerski.github.io/PotionPanic/`
- This GitHub Pages site is read-only by design.
- Persistent board, ticket, and plan editing still happens through
  `npm run docs:ui` or `npm run docs:dev`.

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
8. Run `npm install`.
9. Open the docs board with `npm run docs:ui`.

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

Use these bootstrap commands if you need to install or verify the package-backed
docs tooling:

```powershell
npm install
npm run docs:ui
```

Useful npm commands:

```powershell
npm run docs:dev
npm run docs:dev:local
npm run docs:build
npm test
```

`npm install` downloads the private `@gabrielwawerski/docboard` dependency from
GitHub. Configure GitHub SSH access with read permission first. `npm run docs:ui`
calls the package CLI to start the VitePress server when needed and open the
editable PotionPanic board.

Published read-only site:

```text
https://gabrielwawerski.github.io/PotionPanic/
```

GitHub Pages deployment note:

- In `Settings > Pages`, set the source to `GitHub Actions`.
- If the repository has never had Pages enabled, do that once manually before
  relying on the deploy workflow.

Optional Windows startup management:

```powershell
npm run docs:startup:install
npm run docs:startup:uninstall
npm run docs:stop
```

Repository rules:

- PowerShell is the primary shell expected for local project commands.
- The browser board is the default human task workflow for this repo.
- Persistent webpage editing happens through the local VitePress server, not the
  static build.
- `npm run docs:ticket -- "Ticket title" --assignee "Name"` is available for
  CLI ticket creation when needed, and it now uses the same multi-section
  ticket template as the board workflow.
- AI collaborators should follow the committed `AGENTS.md`, `CLAUDE.md`, and
  `GEMINI.md` instructions.

### Docs commands

- `npm install` installs or verifies the Node dependencies needed for the
  package-backed VitePress docs workflow.
- `npm run docs:ui` uses the Docboard package CLI to start the local docs
  server if needed and open the editable task board in the default browser.
- `npm run docs:dev` starts the LAN-accessible VitePress dev server manually.
- `npm run docs:dev:local` starts the local-only VitePress dev server on
  `127.0.0.1:6420`.
- `npm run docs:ticket -- "Ticket title" --assignee "Name"` uses the Docboard
  package CLI to create a markdown ticket from the board frontmatter settings.
- `npm run docs:startup:install` installs the optional Windows sign-in shortcut
  that starts `npm run docs:dev` in the background without opening a
  browser.
- `npm run docs:startup:uninstall` removes that Windows startup shortcut.
- `npm run docs:stop` stops the docs server process that is listening on the
  shared docs port `6420`.
