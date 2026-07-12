# VitePress Windows Startup Refactor Plan

## Summary

Refactor the docs launch workflow so manual browser opening and Windows startup
autolaunch are cleanly separated.

Use a single Windows-only PowerShell entrypoint at
`Scripts/docs/install-windows-startup.ps1` for both install and uninstall. Add
`docs:startup:install` and `docs:startup:uninstall` npm commands. Keep
`docs:dev` as the current LAN-accessible server, add `docs:dev:local` for
`127.0.0.1:6420`, and make the Startup entry run only
`npm run docs:dev:local`.

Choose a per-user Startup-folder shortcut instead of Task Scheduler. It is the
simpler, no-elevation, repo-local option and matches the repo's archived
precedent.

## Command Surface

- `package.json`
  - Keep `docs:dev` as
    `vitepress dev Docs --host 0.0.0.0 --port 6420`.
  - Add `docs:dev:local` as
    `vitepress dev Docs --host 127.0.0.1 --port 6420`.
  - Keep `docs:ui` as the manual browser-opening path.
  - Add `docs:startup:install` to call
    `Scripts/docs/install-windows-startup.ps1` in install mode.
  - Add `docs:startup:uninstall` to call the same script with `-Uninstall`.

- `Scripts/docs/install-windows-startup.ps1`
  - Default behavior: install or replace the Startup entry.
  - Uninstall behavior: `-Uninstall`.
  - Fail immediately outside Windows with a clear message.
  - Resolve the per-user Startup folder with
    `[Environment]::GetFolderPath('Startup')`.
  - Create one canonical shortcut only, for example
    `PotionPanic - Start Docs Server.lnk`.
  - Remove or replace the canonical shortcut on reinstall so repeated installs
    are idempotent.
  - Also remove known legacy shortcut names from the older backlog workflow
    during install/uninstall to avoid duplicate autostarts after migration.

## Implementation Changes

- Startup shortcut behavior
  - Target `powershell.exe`, not a browser and not `cmd /c start`.
  - Use PowerShell-safe quoting by generating an `-EncodedCommand` payload
    instead of embedding a fragile raw command string.
  - The encoded command should:
    - `Set-Location -LiteralPath <repo root>`
    - run `npm run docs:dev:local`
  - Include `-NoProfile`, `-ExecutionPolicy Bypass`, and `-WindowStyle Hidden`
    in shortcut arguments so startup remains server-only and does not open a
    browser window.
  - Do not call `npm run docs:ui`, `Scripts/docs/open-board.mjs`,
    `Start-Process` for a browser, or any browser-opening command.

- Manual browser-opening path
  - Leave `Scripts/docs/open-board.mjs` and the `docs-ui-launcher` flow as the
    human/manual launcher.
  - Keep its current behavior of starting `docs:dev`, not `docs:dev:local`.
  - Preserve the existing board URL contract at
    `http://127.0.0.1:6420/board`.

- Documentation alignment
  - Update `README.md` and [Docs/onboarding/getting-started.md](../../onboarding/getting-started.md) to explain:
    - `docs:dev` = LAN-accessible manual server
    - `docs:dev:local` = local-only server
    - `docs:ui` = manual browser-opening command
    - `docs:startup:install` / `docs:startup:uninstall` = optional Windows
      startup management
    - Windows startup only starts the local server in the background and never
      opens the browser
  - Update stale repo instruction docs that still reference removed PowerShell
    launchers so the documented workflow is internally consistent.

## Test Plan

- Existing JS tests
  - Run `npm test`.
  - Keep or tighten the existing `docs-ui-launcher` assertions that `docs:ui`
    still starts `docs:dev`.

- PowerShell startup verification
  - Add a focused regression harness for the installer script using a temporary
    Startup-folder path override or equivalent injectable path.
  - Verify install creates exactly one shortcut with the canonical name.
  - Verify reinstall updates/replaces that shortcut and does not create
    duplicates.
  - Verify uninstall removes the canonical shortcut and known legacy shortcut
    names.
  - Verify the shortcut target is `powershell.exe`.
  - Verify shortcut arguments include `npm run docs:dev:local`.
  - Verify shortcut arguments do not include `docs:ui`, `open-board.mjs`,
    `cmd /c start`, `Start-Process`, or a browser-open command.
  - Verify non-Windows execution returns the clear Windows-only error.

- Manual smoke checks
  - Run `npm run docs:dev:local` and confirm the server binds to
    `127.0.0.1:6420`.
  - Run `npm run docs:ui` and confirm the manual launcher still opens the board
    and preserves current behavior.
  - Run `npm run docs:build` to confirm the docs site still builds cleanly
    after doc updates.

## Assumptions

- Use one committed PowerShell script with install and uninstall modes, not
  separate install/uninstall scripts.
- Use symmetric npm command names: `docs:startup:install` and
  `docs:startup:uninstall`.
- Use a Startup-folder shortcut, not Task Scheduler and not a Windows service.
- Keep `docs:ui` as the only browser-opening workflow.
- Use PowerShell-compatible absolute paths and encoded arguments to avoid
  quoting bugs with spaces in the repo path.
