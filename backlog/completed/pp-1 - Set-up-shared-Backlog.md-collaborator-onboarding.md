---
id: PP-1
title: Set up shared Backlog.md collaborator onboarding
status: Done
assignee:
  - Codex
created_date: '2026-06-24 22:49'
updated_date: '2026-06-25 01:35'
labels: []
dependencies: []
references:
  - 'backlog://workflow/overview'
  - 'backlog://workflow/task-creation'
documentation:
  - Docs/plans/backlog-md-collaborator-setup-plan.md
  - AGENTS.md
  - CLAUDE.md
  - GEMINI.md
modified_files:
  - README.md
  - scripts/setup-backlog.ps1
  - scripts/open-backlog-board.ps1
  - scripts/install-backlog-autolaunch.ps1
  - scripts/remove-backlog-autolaunch.ps1
  - .run/Open Backlog Board.run.xml
  - .gitignore
  - backlog
priority: medium
ordinal: 1000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Make Backlog.md a repo-standard collaborator workflow so a fresh clone can install the CLI, wire MCP for Codex or Gemini, open the browser board, and reuse the same committed project state and launch helpers.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The repository tracks shared Backlog.md project state and does not ignore the entire `backlog/` tree.
- [x] #2 A top-level collaborator onboarding document explains the supported Backlog.md install, MCP setup, browser UI flow, and the rule not to run `backlog init` in this repository.
- [x] #3 PowerShell setup and board-launch scripts exist and match the documented non-destructive bootstrap behavior.
- [x] #4 Windows auto-launch install and removal scripts manage a per-user Startup shortcut that launches the shared board opener script.
- [x] #5 A shared Rider run configuration opens the Backlog board through the repo-local PowerShell launcher without hard-coded machine-local URLs.
- [x] #6 Documentation and scripts stay aligned with the existing agent instruction files and with the committed plan.
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. Keep the task scoped to the collaborator setup plan and avoid unrelated dirty files already present on `master`.
2. Run a focused pre-change verification pass to confirm the current gaps: missing top-level onboarding README, missing PowerShell helper scripts, missing shared Rider run configuration, and current Git tracking state for `backlog/`.
3. Implement the onboarding entry point and MCP setup guidance in `README.md`, aligned with the existing `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md` instructions.
4. Add repo-local PowerShell helpers for bootstrap, board launching, and per-user Startup shortcut install/remove behavior without auto-configuring clients or rewriting repo config.
5. Add a project-level Rider run configuration under `.run/` that shells through the repo-local board launcher from the repository root.
6. Verify the new files with focused command checks and script dry-run style validation where practical, then update acceptance criteria and final notes in Backlog.
<!-- SECTION:PLAN:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Implemented repo-standard Backlog onboarding with a new top-level README, repo-local PowerShell helpers, a shared Rider `.run` configuration, and tracked `backlog/` scaffolding files for empty shared-state directories.

Verification completed with non-intrusive checks: `setup-backlog.ps1` was exercised against fake `backlog` and fake `npm` commands, `open-backlog-board.ps1` was exercised against a fake `backlog` command and confirmed to invoke `board`, the install/remove autolaunch scripts were exercised against a temporary `%APPDATA%` and confirmed to create then remove the shortcut, all PowerShell scripts parsed successfully, `.gitignore` ignores `backlog/.locks/*`, and the tracked task file remains unignored.

Did not run a real `backlog board` browser launch or a full Unity test suite because this change is limited to onboarding docs/config/scripts and the browser command would open an interactive UI.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added collaborator-facing Backlog.md onboarding to the repository. `README.md` now documents the exact bootstrap commands, MCP-first setup for Codex and Gemini CLI, the generic `mcpServers.backlog` JSON, and the rule not to run `backlog init` in this repo. Added repo-local PowerShell helpers to install `backlog.md` when missing, open the Backlog board, and install/remove a per-user Startup shortcut that launches the board opener through PowerShell. Added a shared Rider run configuration under `.run/` that invokes `scripts/open-backlog-board.ps1` from the project root, and updated `.gitignore` to ignore only `backlog/.locks/` while adding tracked `.gitkeep` scaffolding so empty Backlog directories can ship in Git.

Verification: exercised `scripts/setup-backlog.ps1` against fake `backlog` and fake `npm` commands, exercised `scripts/open-backlog-board.ps1` against a fake `backlog` command and confirmed it invoked `board`, exercised the autolaunch install/remove scripts against a temporary `%APPDATA%` and confirmed the shortcut was created then removed, parsed all PowerShell scripts successfully, confirmed the new text files are UTF-8 without BOM, and confirmed `backlog/.locks/*` is ignored while the tracked Backlog task file is not.
<!-- SECTION:FINAL_SUMMARY:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [x] #1 Tests pass
- [x] #2 Documentation updated
- [x] #3 No regressions introduced
<!-- DOD:END -->
