---
title: Rename browser UI launcher to backlog-ui
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/pp-1.1.md'
---

## Description

Replace the old Backlog browser launcher surface with a canonical `scripts/backlog-ui.ps1`
command and update the shared Rider run configuration and onboarding docs to treat that
browser-opening command as the default human entry point.

## Acceptance Criteria


- [x] #1 A canonical `scripts/backlog-ui.ps1` launcher exists and opens the browser-based
  Backlog UI.
- [x] #2 The previous `scripts/open-backlog-board.ps1` entry point is replaced cleanly
  without breaking the current repo-local launcher flow.
- [x] #3 The shared Rider run configuration uses the canonical browser UI launcher.
- [x] #4 README guidance treats the browser UI launcher as the default human path and uses
  the new command name consistently.


## Implementation Plan


1. Run a failing pre-check to confirm `scripts/backlog-ui.ps1` does not yet exist and the
   Rider configuration still points at `scripts/open-backlog-board.ps1`.
2. Add `scripts/backlog-ui.ps1` as the canonical browser UI launcher by reusing the
   current browser-opening behavior.
3. Replace `scripts/open-backlog-board.ps1` with a thin compatibility wrapper that
   delegates to `scripts/backlog-ui.ps1` so existing local flows keep working.
4. Update `.run/Open Backlog Board.run.xml` and `README.md` to use
   `scripts/backlog-ui.ps1` as the default human browser UI path.
5. Verify red-green behavior with file-path checks, PowerShell parse checks, and stubbed
   `backlog` command invocations for both the canonical launcher and the compatibility
   wrapper.


## Implementation Notes

Ran a red pre-check before editing: `scripts/backlog-ui.ps1` was missing, the shared Rider
run configuration still pointed at `scripts/open-backlog-board.ps1`, and README did not
mention the new canonical command.

Implemented `scripts/backlog-ui.ps1` as the canonical browser UI launcher, converted
`scripts/open-backlog-board.ps1` into a compatibility wrapper, updated the Rider run
configuration to call `backlog-ui.ps1`, and repointed the autolaunch installer to the
canonical script.

Verification: confirmed file/reference checks pass, parsed the updated PowerShell scripts
successfully, exercised both launcher names against a fake `backlog` command and confirmed
both invoke `board`, and confirmed the Startup shortcut arguments now target
`scripts/backlog-ui.ps1`.

## Final Summary

Promoted `scripts/backlog-ui.ps1` to the canonical browser UI command for Backlog in this
repo. The new script contains the direct `backlog board` launcher logic,
`scripts/open-backlog-board.ps1` now delegates to it as a compatibility wrapper, the
shared Rider run configuration now calls `scripts/backlog-ui.ps1`, and the autolaunch
installer now writes a Startup shortcut that targets the canonical script. `README.md` now
uses `./scripts/backlog-ui.ps1` as the default human browser UI command and describes the
old script as a compatibility wrapper.

Verification: ran a failing pre-check before the change, then confirmed the new script
exists, Rider and README reference it, all updated PowerShell scripts parse successfully,
both launcher names invoke `backlog board` under a fake `backlog` stub, and the generated
Startup shortcut arguments point at `C:\Dev\PotionPanic\scripts\backlog-ui.ps1`.

## Definition of Done


- [x] #1 Tests pass
- [x] #2 Documentation updated
- [x] #3 No regressions introduced

