---
title: Make backlog-ui open browser URL and ensure server is running
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/pp-1.2.md'
---

## Description

Change the canonical Backlog UI launcher so it opens the Backlog browser URL in
the system default browser instead of running `backlog board`, and make it start
the local
`backlog browser --no-open` server automatically when the UI server is not
already running.

## Acceptance Criteria

- [x] #1 `scripts/backlog-ui.ps1` opens the Backlog browser URL in the system
  default browser instead of invoking `backlog board`.
- [x] #2 If the local Backlog browser server is not responding,
  `scripts/backlog-ui.ps1`
  starts `backlog browser --no-open` on the configured port and waits for the
  URL to become available before opening it.
- [x] #3 The launcher reads the configured Backlog port from repo configuration
  and falls back safely when the port is absent.
- [x] #4 README guidance describes the script as opening the browser URL in the
  default browser and ensuring the local server is running.

## Implementation Plan

1. Run a failing pre-check to confirm `scripts/backlog-ui.ps1` still shells out
   to
   `backlog board` and does not contain direct URL launcher behavior.
2. Replace `scripts/backlog-ui.ps1` with a script that reads `default_port` from
   `backlog.config.yml` with a safe fallback, probes the local browser URL,
   starts
   `backlog browser --no-open --port <port>` hidden if needed, waits for the URL
   to respond, and then opens the URL in the system default browser.
3. Keep `scripts/open-backlog-board.ps1` as a compatibility wrapper to the
   canonical launcher.
4. Update `README.md` so the documented default human path is the browser URL
   launcher behavior rather than `backlog board` semantics.
5. Verify with red-green command checks, PowerShell parse checks, and isolated
   stub runs that prove the launcher starts the expected background command and
   opens the expected URL without relying on a real browser session.

## Implementation Notes

Red pre-check confirmed the old implementation still invoked `backlog board`,
did not contain the direct URL launcher flow, and README wording did not
describe the ensure-server-then-open-URL behavior.

Replaced `scripts/backlog-ui.ps1` with a direct browser URL launcher that reads
`default_port` from `backlog.config.yml`, probes `http://localhost:<port>`,
starts
`backlog browser --no-open --port <port>` hidden when needed, waits for
readiness, and opens the URL through the system default browser.

Verification: parsed the updated PowerShell scripts successfully; exercised a
stub scenario where the server was not ready and confirmed the launcher started
`backlog browser --no-open --port 6420` before opening `http://localhost:6420`;
exercised a stub scenario where the server was already ready and confirmed it
opened the URL without starting a background server; exercised a copied launcher
with no config file present and confirmed it still opened
`http://localhost:6420`; verified README wording reflects the new behavior.

## Final Summary

Changed the canonical Backlog launcher to open the Backlog browser URL directly
in the system default browser instead of delegating to `backlog board`.
`scripts/backlog-ui.ps1`
now reads the configured port from `backlog.config.yml`, probes the local URL,
starts
`backlog browser --no-open --port <port>` hidden if the browser server is not
already responding, waits for readiness, and then opens
`http://localhost:<port>`. The compatibility wrapper at
`scripts/open-backlog-board.ps1` continues to delegate to the canonical
launcher, so the existing Rider run configuration and any old script callers
inherit the new behavior without further changes.

Verification: confirmed the updated scripts parse cleanly; verified a not-ready
stub path that starts `backlog browser --no-open --port 6420` and then opens
`http://localhost:6420`; verified an already-ready stub path that opens the URL
without starting a server; verified a no-config fallback path using a temporary
script copy outside the repo that still opened `http://localhost:6420`; and
confirmed README now describes the ensure-server-then-open-default-browser
behavior.

## Definition of Done

- #1 Tests pass
- #2 Documentation updated
- #3 No regressions introduced

