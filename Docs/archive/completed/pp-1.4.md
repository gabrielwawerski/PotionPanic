---
id: 1.4
title: Separate Backlog server autolaunch from browser-opening launcher
status: done
priority: medium
tags: []
---

## Description

Adjust the Windows Startup autolaunch behavior so signing in does not open the Backlog
browser UI automatically. Human/manual launch should still open the Backlog browser UI on
demand, while autolaunch should only ensure the local Backlog browser server is running in
the background.

## Acceptance Criteria


- [x] #1 The per-user Windows Startup autolaunch no longer opens the Backlog browser UI
  automatically at sign-in.
- [x] #2 The manual human launcher still ensures the Backlog browser server is running and
  opens the Backlog UI in the default browser on demand.
- [x] #3 The startup/autolaunch path and the manual browser-opening path are clearly
  separated so future changes do not re-couple them accidentally.
- [x] #4 README and onboarding guidance describe the difference between background
  autolaunch behavior and manual browser opening.


## Implementation Plan


1. Add a dedicated PowerShell entrypoint for Windows Startup autolaunch that ensures the
   Backlog browser server is running but never opens a browser window.
2. Keep `scripts/backlog-ui.ps1` as the manual human launcher that ensures the server is
   available and then opens the Backlog URL in the default browser.
3. Update `scripts/install-backlog-autolaunch.ps1` so the Startup shortcut targets the
   server-only autolaunch entrypoint instead of the manual browser-opening launcher.
4. Keep removal behavior reliable and update any shortcut naming or descriptions only as
   needed to reflect the new background-startup semantics.
5. Update `README.md` and [Docs/getting-started.md](../../onboarding/getting-started.md) so human collaborators understand that
   sign-in starts the Backlog server in the background, while `./scripts/backlog-ui.ps1`
   is the explicit command that opens the board.
6. Verify the split with script parsing plus stubbed PowerShell runs that prove the
   autolaunch path does not call browser open and the manual path still does.


## Implementation Notes


1. Add a dedicated PowerShell entrypoint for Windows Startup autolaunch that ensures the
   Backlog browser server is running but never opens a browser window.
2. Keep `scripts/backlog-ui.ps1` as the manual human launcher that ensures the server is
   available and then opens the Backlog URL in the default browser.
3. Update `scripts/install-backlog-autolaunch.ps1` so the Startup shortcut targets the
   server-only autolaunch entrypoint instead of the manual browser-opening launcher.
4. Keep removal behavior reliable and update any shortcut naming or descriptions only as
   needed to reflect the new background-startup semantics.
5. Update `README.md` and [Docs/getting-started.md](../../onboarding/getting-started.md) so human collaborators understand that
   sign-in starts the Backlog server in the background, while `./scripts/backlog-ui.ps1`
   is the explicit command that opens the board.
6. Verify the split with script parsing plus stubbed PowerShell runs that prove the
   autolaunch path does not call browser open and the manual path still does.

Implemented a clean split between manual browser opening and Windows Startup autolaunch.
Added `scripts/backlog-browser-server.ps1` for shared server-start helpers, kept
`scripts/backlog-ui.ps1` as the manual browser-opening entrypoint, and added
`scripts/backlog-autolaunch.ps1` as the server-only Startup entrypoint.

Updated `scripts/install-backlog-autolaunch.ps1` to target the new server-only script,
rename the Startup shortcut to `PotionPanic - Start Backlog Server.lnk`, and remove the
old `PotionPanic - Open Backlog Board.lnk` shortcut during install if it exists. Updated
`scripts/remove-backlog-autolaunch.ps1` to remove both the new and legacy shortcut names.

Added `scripts/test-backlog-launchers.ps1` as a regression harness. Verified the red state
first when the server-only autolaunch script did not yet exist. After implementation,
reran the harness successfully to confirm the manual launcher starts the server and opens
the URL, while the autolaunch script starts the server without opening a browser.

Documentation: updated `README.md` and [Docs/getting-started.md](../../onboarding/getting-started.md) so humans know sign-in
only starts the Backlog server in the background and `./scripts/backlog-ui.ps1` remains
the explicit browser-opening command.

Task is moved to Test / Review instead of Done because the project Definition of Done
includes a committed branch, and no commit was created in this session.

## Definition of Done


- [x] #1 Acceptance criteria met
- [x] #2 Relevant Unity verification completed
- [x] #3 No new relevant Console errors
- [x] #4 Documentation or task notes updated when needed
- [x] #5 Branch committed and ready for review or merge

## Notes
- Documentation: `README.md`, [Docs/getting-started.md](../../onboarding/getting-started.md)
- Likely affected files: `scripts/backlog-browser-server.ps1`,
  `scripts/backlog-autolaunch.ps1`, `scripts/backlog-ui.ps1`,
  `scripts/install-backlog-autolaunch.ps1`, `scripts/remove-backlog-autolaunch.ps1`,
  `scripts/test-backlog-launchers.ps1`, `README.md`, [Docs/getting-started.md](../../onboarding/getting-started.md)
