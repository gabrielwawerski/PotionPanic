---
title: Fix backlog-ui background server startup on PowerShell shim installs
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/pp-1.3.md'
---

## Description

Fix the canonical Backlog UI launcher so it can start the Backlog browser server reliably
on Windows installations where `Get-Command backlog` resolves to a PowerShell shim script
such as `backlog.ps1`, which `Start-Process` cannot launch directly with the current
implementation.

## Acceptance Criteria


- [x] #1 `scripts/backlog-ui.ps1` starts the Backlog browser server reliably when
  `backlog` resolves to a PowerShell script shim.
- [x] #2 The launcher still works when the Backlog server is already running and when the
  configured browser URL is already available.
- [x] #3 The fix does not change the user-facing command or Rider run configuration
  contract.


## Implementation Plan


1. Use the current `powershell.exe -File scripts/backlog-ui.ps1` failure as the red
   reproduction and confirm the root cause: `Get-Command backlog` resolves to an
   `ExternalScript` PowerShell shim (`backlog.ps1`) that `Start-Process -FilePath <shim>`
   cannot launch directly with the current arguments.
2. Add a launcher helper in `scripts/backlog-ui.ps1` that starts the Backlog browser
   server differently based on the resolved command type/path: use
   `powershell.exe -NoProfile -ExecutionPolicy Bypass -File <shim>` for `.ps1` shims, and
   keep direct `Start-Process` for executable/cmd-style commands.
3. Keep the user-facing launcher contract unchanged: same script entry point, same
   readiness loop, same URL opening behavior.
4. Verify green with the original failing command, plus targeted harness checks for the
   shim-launch path and the already-ready URL path.


## Implementation Notes

Root cause investigation confirmed the failure was not in the readiness loop. On this
machine `Get-Command backlog` resolves to `C:\nvm4w\nodejs\backlog.ps1` as an
`ExternalScript`, and `Start-Process -FilePath <that .ps1>` throws
`InvalidOperationException` instead of starting the background server.

Implemented a dedicated `Start-BacklogBrowserServer` helper in `scripts/backlog-ui.ps1`.
It now launches PowerShell shims through
`powershell.exe -NoProfile -ExecutionPolicy Bypass -File <shim> browser --no-open --port <port>`,
while preserving the direct `Start-Process` path for non-script command targets.

Verification: parsed `scripts/backlog-ui.ps1` successfully; confirmed
`Start-Process powershell.exe ... -File C:\nvm4w\nodejs\backlog.ps1 browser --help` exits
with code 0 on this machine; exercised a harness where the UI was initially unavailable
and confirmed the launcher starts `powershell.exe` with the expected shim arguments before
opening `http://localhost:6420`; exercised a harness where the UI was already ready and
confirmed it opens the URL without trying to start another server; updated README wording
to match the launcher behavior.

## Final Summary

Fixed `scripts/backlog-ui.ps1` for Windows installs where the `backlog` command resolves
to a PowerShell shim script instead of a directly executable command. The original failure
came from trying to run `Start-Process -FilePath C:\nvm4w\nodejs\backlog.ps1 ...`, which
PowerShell cannot launch that way. The launcher now detects script-shim targets and starts
them through
`powershell.exe -NoProfile -ExecutionPolicy Bypass -File <shim> browser --no-open --port <port>`.
Non-script command targets still use the direct `Start-Process` path, and the user-facing
launcher contract, URL probing, and browser opening flow remain unchanged.

Verification: reproduced the original failure before the fix; confirmed the updated script
parses cleanly; confirmed the direct `powershell.exe ... -File backlog.ps1 browser --help`
launch path exits successfully on this machine; verified with a harness that the launcher
starts the shim-backed server correctly when the UI is not ready and still only opens
`http://localhost:6420` when the UI is already responding; and updated README wording to
reflect that the script starts the server if needed before opening the URL.

## Definition of Done


- [x] #1 Tests pass
- [x] #2 Documentation updated
- [x] #3 No regressions introduced

