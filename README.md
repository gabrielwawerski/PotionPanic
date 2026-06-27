# PotionPanic

Potion Panic is a small-scope Unity project for a two-person beginner team. The target is
a finished small game, not a large experimental prototype.

This repository also uses Backlog.md as shared project state for planning and execution.
The committed `backlog/` directory and `backlog.config.yml` are part of the project, so
each clone starts from the same board, tasks, and workflow instructions.

## Start Here

If this is your first day in the repo, read these in order:

1. `Docs/getting-started.md`
2. `Docs/team-workflow-guide.md`
3. `Docs/Potion Panic.md`
4. `Docs/Potion Panic - Technical Architecture.md`

## First Day Setup

Install these once on each machine:

- Unity Hub
- Unity Editor `6000.5.1f1`
- JetBrains Rider
- Git
- Git LFS
- Node.js and npm for Backlog.md tooling

Then:

1. Clone the repository.
2. Run `git lfs install`.
3. Open the repo root in Unity Hub with editor `6000.5.1f1`.
4. Let Unity finish package import and script compilation.
5. Open `PotionPanic.sln` in Rider.
6. Open `Assets/Scenes/SampleScene.unity`.
7. Press Play and confirm the project enters Play Mode without new console errors.
8. Open the Backlog board with `.\scripts\backlog-ui.ps1`.

Current repo note:

- The shared prototype scene is currently `Assets/Scenes/SampleScene.unity`.
- `Assets/Scenes/testscene.unity` is not the shared milestone scene unless a task
  explicitly says so.
- Milestone 1 is expected to rename or replace the shared scene as `Laboratory.unity`.

## Collaboration Docs

- `Docs/getting-started.md` is the step-by-step onboarding guide.
- `Docs/team-workflow-guide.md` is the day-to-day collaboration guide.
- `Docs/Potion Panic.md` is the game design source of truth.
- `Docs/Potion Panic - Technical Architecture.md` is the runtime structure source of
  truth.
- `Docs/plans/implementation-readiness-review.md` records locked MVP decisions and current
  repo notes.

## Backlog.md Setup

Use these bootstrap commands if you need to install or connect the shared Backlog tooling:

```powershell
npm i -g backlog.md
codex mcp add backlog backlog mcp start
.\scripts\backlog-ui.ps1
```

Human collaborators who just need the board in a browser can usually stop at:

```powershell
.\scripts\setup-backlog.ps1
.\scripts\backlog-ui.ps1
```

Repository rules:

- This repository is already initialized. Do not run `backlog init` here.
- `npm i -g backlog.md` is the default install path for this project.
- PowerShell is the primary shell expected for local project commands.
- MCP is the default integration path for Codex and Gemini CLI collaborators.
- The browser is the direct Backlog UI for humans.
- Direct CLI task workflows are not the primary collaborator path in this repo.
- AI collaborators should follow the committed `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md` instructions.

### Manual MCP setup

Codex:

```powershell
codex mcp add backlog backlog mcp start
```

Gemini CLI:

```powershell
gemini mcp add backlog -- backlog mcp start
```

Generic MCP configuration:

```json
{
  "mcpServers": {
    "backlog": {
      "command": "backlog",
      "args": ["mcp", "start"]
    }
  }
}
```

### Shared helper scripts

- `.\scripts\backlog-ui.ps1` is the default human command, starts the local Backlog browser server if needed, and opens the Backlog URL in the default installed browser.
- `.\scripts\setup-backlog.ps1` installs `backlog.md` if needed and prints the supported MCP setup commands.
- `.\scripts\open-backlog-board.ps1` remains as a compatibility wrapper for the canonical browser UI launcher.
- `.\scripts\install-backlog-autolaunch.ps1` installs a per-user Windows Startup shortcut
  that starts the Backlog browser server in the background at sign-in without opening a
  browser window.
- `.\scripts\remove-backlog-autolaunch.ps1` removes that Startup shortcut, including the
  older board-opening shortcut name.
