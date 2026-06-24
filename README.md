# PotionPanic

This repository uses Backlog.md as shared project state for planning and execution. The committed `backlog/` directory and `backlog.config.yml` are part of the project, so each clone starts from the same board, tasks, and workflow instructions.

## Backlog.md Setup

Use these bootstrap commands:

```powershell
npm i -g backlog.md
codex mcp add backlog backlog mcp start
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
- `.\scripts\install-backlog-autolaunch.ps1` installs a per-user Windows Startup shortcut for the board launcher.
- `.\scripts\remove-backlog-autolaunch.ps1` removes that Startup shortcut.
