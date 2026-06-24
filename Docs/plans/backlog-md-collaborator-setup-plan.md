# Backlog.md Collaborator Setup Plan

## Summary

Make Backlog.md a repo-standard tool that a new collaborator can use in under five minutes. Use MCP-first onboarding for Codex and Gemini CLI collaborators, use the browser as the direct Backlog UI, and commit Backlog state so every clone receives the same board, tasks, and workflow instructions.

## Current State

- `backlog.config.yml` is already tracked and points the project at `backlog/`.
- `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md` already instruct AI collaborators to read `backlog://workflow/overview`.
- `backlog/` exists locally, but it is not currently tracked in Git in this workspace.
- There is no top-level `README.md`, so human collaborator onboarding does not have a single starting point yet.

## Key Changes

### 1. Commit Backlog state as shared project data

- Add `backlog/**` to Git so tasks, docs, decisions, milestones, and completed items ship with the repository.
- Keep `backlog.config.yml` tracked as the canonical project configuration.
- Treat Backlog content as shared project state, not per-user local setup.
- If `backlog/.locks` becomes noisy, exclude only lock artifacts rather than excluding the entire `backlog/` tree.

### 2. Add a top-level collaborator entry point

- Create `README.md` with a short `Backlog.md Setup` section.
- Use these exact bootstrap commands:

```powershell
npm i -g backlog.md
codex mcp add backlog backlog mcp start
backlog board
```

- State these rules explicitly:
  - This repository is already initialized; collaborators should not run `backlog init` here.
  - `npm i -g backlog.md` is the default install path for this project.
  - PowerShell is the primary shell expected for local project commands.
  - MCP is the default integration path for Codex and Gemini CLI collaborators.
  - The browser is the direct Backlog UI for humans.
  - Direct CLI task workflows are not the primary collaborator path in this repo.
  - AI tools should follow the committed `AGENTS.md`, `CLAUDE.md`, or `GEMINI.md` instructions already present in the repo.

### 3. Add a one-command PowerShell bootstrap script

- Create `scripts/setup-backlog.ps1`.
- Script behavior:
  - Check whether the `backlog` CLI is available.
  - If missing, install it with `npm i -g backlog.md`.
  - If present, do nothing beyond confirming the command exists.
  - Print the MCP setup commands for both supported agent clients:

```powershell
codex mcp add backlog backlog mcp start
gemini mcp add backlog -- backlog mcp start
```

- The script should not:
  - run `backlog init`
  - rewrite repo config
  - call `backlog --version`
  - call `backlog instructions overview`
  - auto-configure MCP clients unless the team explicitly decides to permit that later
  - auto-open the browser unless the team explicitly decides to permit that later

### 4. Add browser-launch helpers for Windows

- Create `scripts/open-backlog-board.ps1`.
- Script behavior:
  - Confirm the `backlog` command exists.
  - Run `backlog board`.
  - Exit with a clear error if the CLI is missing.
- Use this script as the single shared launcher for both Windows auto-launch and Rider.

- Create `scripts/install-backlog-autolaunch.ps1`.
- Recommended implementation:
  - Register a per-user Windows Startup entry rather than a machine-wide Scheduled Task.
  - Create or update a shortcut in `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`.
  - Point that shortcut at `powershell.exe` with arguments that run `scripts/open-backlog-board.ps1`.
  - Use a minimized or hidden window style so login does not leave an extra terminal open.
  - Keep the install per-user and non-admin.
- Add a matching removal script if cleanup is desired later:
  - `scripts/remove-backlog-autolaunch.ps1`

### 5. Add a shareable Rider run configuration

- Create a Rider run configuration that opens the Backlog browser page by calling `scripts/open-backlog-board.ps1`.
- Store it as a project file, not in `workspace.xml`, so collaborators receive it automatically.
- Prefer the project-level `.run/` directory for the shared file instead of the currently ignored `.idea/.../workspace.xml` path.
- Name it clearly, for example: `Open Backlog Board`.
- The run configuration should:
  - use PowerShell as the executable
  - call the repo-local launcher script
  - run from the repository root
  - open the default browser through `backlog board`, not through a hard-coded local URL

### 6. Document MCP setup as the primary collaborator path

- Add a short setup section in `README.md` or `Docs/team-workflow-guide.md`.
- Use the shared server name `backlog`.
- For Codex, document this manual command:

```powershell
codex mcp add backlog backlog mcp start
```

- For Gemini CLI, document this manual command:

```powershell
gemini mcp add backlog -- backlog mcp start
```

- For generic MCP configuration, document:

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

- Make it explicit that MCP is the expected agent integration path and the browser is the expected human UI path.

## Public Interfaces and Files

- Track: `backlog/**`
- Keep: `backlog.config.yml`
- Create: `README.md`
- Create: `scripts/setup-backlog.ps1`
- Create: `scripts/open-backlog-board.ps1`
- Create: `scripts/install-backlog-autolaunch.ps1`
- Optional create: `scripts/remove-backlog-autolaunch.ps1`
- Create: `.run/Open Backlog Board.run.xml`
- Keep unchanged unless requirements change: `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`

## Verification

- Fresh clone or clean user profile:
  - Run `npm i -g backlog.md`
  - Add the `backlog` MCP server to Codex or Gemini CLI.
  - Confirm the client can read `backlog://workflow/overview`.
  - Run `backlog board`.
  - Confirm the browser-based Backlog UI opens successfully.
- Script verification:
  - Run `scripts/open-backlog-board.ps1`.
  - Confirm it opens the default browser to the Backlog board.
  - Run `scripts/install-backlog-autolaunch.ps1`.
  - Confirm a Startup entry is created for the current user.
  - Sign out and sign back in, then confirm the Backlog board opens automatically.
- Rider verification:
  - Open the shared `Open Backlog Board` run configuration in Rider.
  - Run it without editing local paths.
  - Confirm it opens the default browser successfully.

## Assumptions

- Collaborators have Node.js and npm available.
- This project uses MCP for Codex and Gemini CLI collaborators.
- The browser is the intended direct Backlog UI.
- Windows auto-launch only needs per-user setup, not machine-wide setup.
- Rider collaborators can consume project-file run configurations from `.run/`.
- `backlog/.locks` is operational state and should only be ignored if it produces Git noise.
- The existing agent instruction files remain the canonical AI onboarding mechanism.
