# PotionPanic

Potion Panic is a small-scope Unity game for a two-person team learning to ship
a complete project. The target is a polished, replayable crisis-management game
inside one magical laboratory.

Gameplay code is still at the scaffold stage. The repository currently contains
the Unity project foundation, accepted game and runtime contracts, project
management documentation, and an implemented editor-only Coordination system
for reducing conflicts on shared scenes.

## Start here

Use the published site to read project documentation:

- <https://gabrielwawerski.github.io/PotionPanic/>

Use the local docs server when you need to edit the board, tickets, or plans.
The published site is read-only and cannot write changes back to Markdown.

Read in this order on a new machine:

1. [Project Setup](Docs/onboarding/getting-started.md)
2. [Daily Workflow](Docs/collaboration/team-workflow.md)
3. [Project Overview](Docs/project/index.md)
4. [Guides](Docs/guides/index.md)

Use the [Documentation Atlas](Docs/ATLAS.md) to find the owner of a recurring
question. Use the
[Evergreen Documentation Contract](Docs/evergreen-documentation.md) when
changing long-lived project documentation.

## First commands

Run these commands from the repository root in PowerShell after cloning both
PotionPanic and its required sibling `Docboard` checkout:

```powershell
git lfs install
npm install
npm run docs:ui
```

`package.json` resolves `@gabrielwawerski/docboard` from `../Docboard`. A
checkout without that sibling directory cannot install the docs dependencies.
Project Setup explains the expected folder layout and recovery steps.

Open the repository root in Unity Hub with editor `6000.5.1f1`. The current
shared smoke-test scene is `Assets/Scenes/SampleScene.unity`.
`Laboratory.unity` is the accepted target scene name for Milestone 1, not a
claim that the rename has already happened.

## Documentation commands

```powershell
npm run docs:ui
npm run docs:dev
npm run docs:dev:local
npm run docs:build
npm test
```

- `docs:ui` starts the local server if necessary and opens the editable board.
- `docs:dev` serves the site on the local network at port `6420`.
- `docs:dev:local` binds only to `127.0.0.1:6420`.
- `docs:build` creates the static VitePress site and reports broken routes.
- `npm test` checks docs tooling, configuration, links, and secret safety. It
  does not grade documentation prose.

Optional Windows startup management:

```powershell
npm run docs:startup:install
npm run docs:startup:uninstall
npm run docs:stop
```

GitHub Pages uses the `Deploy Docs` workflow. A new repository must set
`Settings > Pages > Source` to `GitHub Actions` once before the workflow can
publish the site.

## Coordination service

The Unity editor opens the developer tool from
`Window > Potion Panic > Coordination`. Start with the
[Unity Coordination Guide](Docs/guides/coordinated-leasing.md).

The Cloudflare Worker deploy is a manual, authenticated operator action.
GitHub Actions verifies the server but does not receive deployment credentials
or deploy it. Operators use
[`Tools/CoordinationServer/README.md`](Tools/CoordinationServer/README.md).

The client endpoint is stored in `coordination.json`. A configured URL is not
proof that the service is healthy; verify the Worker's `/health` response
before treating it as available.

## Repository rules

- PowerShell is the primary shell for local project commands.
- Human contributors normally use the Git interface in Rider, WebStorm, or
  VS Code; PowerShell Git commands remain the diagnostic and recovery fallback.
- The browser board is the default human task workflow.
- AI collaborators follow `AGENTS.md`; `CLAUDE.md` and `GEMINI.md` point to it.
- Announce before editing scenes, prefabs, project settings, or package files.
- Never commit generated folders such as `Library/`, `Temp/`, `Logs/`, `obj/`,
  `UserSettings/`, `node_modules/`, or VitePress cache and build output.
