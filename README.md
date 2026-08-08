# PotionPanic

Potion Panic is a small-scope Unity project for a two-person beginner team. The
target is a finished small game, not a large experimental prototype.

This repository uses a VitePress site rooted in `Docs/` for shared project
state, planning, and evergreen documentation. The reusable board, sidebar,
ticket, plan, and theme tooling comes from the private
`@gabrielwawerski/docboard` package. This repository owns the Potion Panic docs
content, board frontmatter, and VitePress configuration.

Published read-only docs site:

- `https://gabrielwawerski.github.io/PotionPanic/`
- Persistent board, ticket, and plan editing happens through the local
  VitePress server, usually with `npm run docs:ui`.

## Start Here

Read these in order:

1. `Docs/index.md`
2. `Docs/ATLAS.md`
3. `Docs/onboarding/getting-started.md`
4. `Docs/collaboration/team-workflow.md`
5. `Docs/board.md`
6. `Docs/project/game-design.md`
7. `Docs/project/mvp-scope.md`
8. `Docs/project/technical-architecture.md`

## First Day Commands

Run from the repository root in PowerShell:

```powershell
git lfs install
npm install
npm run docs:ui
```

For Unity, open the repo in Unity Hub with editor `6000.5.1f1`, open
`Assets/Scenes/SampleScene.unity`, press Play, and confirm there are no new
Console errors. `SampleScene.unity` is the current shared prototype scene;
`Laboratory.unity` is the planned canonical gameplay scene after Milestone 1
work completes.

## Documentation Owners

- `Docs/ATLAS.md` routes recurring questions to the owning document.
- `Docs/onboarding/getting-started.md` owns detailed setup instructions.
- `Docs/collaboration/team-workflow.md` owns day-to-day process and protected
  Unity edit coordination.
- `Docs/project/game-design.md` owns player-facing design.
- `Docs/project/mvp-scope.md` owns locked MVP scope, milestone order, and
  definition of done.
- `Docs/project/technical-architecture.md` owns runtime structure.
- `Docs/guides/coordinated-leasing.md` owns Unity Coordination window usage.
- `Tools/CoordinationServer/README.md` owns coordination Worker operations.

## Docs Commands

```powershell
npm run docs:ui
npm run docs:dev
npm run docs:dev:local
npm run docs:build
npm test
```

- `npm run docs:ui` starts the local docs server if needed and opens the
  editable board.
- `npm run docs:dev` starts the LAN-accessible server on port `6420`.
- `npm run docs:dev:local` starts the local-only server on
  `127.0.0.1:6420`.
- `npm run docs:build` verifies the static VitePress site.
- `npm test` runs docs tooling tests.

Optional Windows startup management:

```powershell
npm run docs:startup:install
npm run docs:startup:uninstall
npm run docs:stop
```

GitHub Pages uses the `Deploy Docs` workflow. In `Settings > Pages`, the source
must be `GitHub Actions`; do that once manually if the repository has never
served Pages before.

## Coordination Worker

The coordination Worker deploy is a manual, authenticated operator action.
GitHub Actions is verification-only for the coordination server. It runs type
checking, tests, and a Wrangler dry run without deployment credentials.

`coordination.json` currently names
`https://potion-panic-coordination.gabriel-wawerski.workers.dev`. Verify its
`/health` response before treating the endpoint as available.

Operational procedures live in `Tools/CoordinationServer/README.md`.

## Repository Rules

- PowerShell is the primary shell for local project commands.
- The browser board is the default human task workflow.
- AI collaborators follow `AGENTS.md`; `CLAUDE.md` and `GEMINI.md` point to it.
- Never commit generated folders such as `Library/`, `Temp/`, `Logs/`, `obj/`,
  `UserSettings/`, `node_modules/`, or VitePress cache/dist output.
