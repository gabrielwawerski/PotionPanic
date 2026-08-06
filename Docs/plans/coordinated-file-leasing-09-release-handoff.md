---
title: 'Coordinated Leasing 09: Release Acceptance and Documentation Handoff'
---

# Coordinated Leasing 09: Release Acceptance and Documentation Handoff

**Session goal:** Deploy the backend, prove the full two-machine behavior, then
publish stable operating guidance and close the program.

**Depends on:** Slices 01 through 08.

**Produces:** A release-ready coordination system with evidence recorded in
`PP-7`, updated evergreen docs, and an archived program plan.

## Files and external state

- Modify `coordination.json` to replace the placeholder endpoint after the
  Worker is deployed.
- Update `README.md`, `Docs/onboarding/getting-started.md`,
  `Docs/collaboration/team-workflow.md`, and `Docs/guides/unity/editor-safety.md`.
- Append deployment, test, and two-machine evidence to `Docs/tickets/PP-7.md`.
- Create Cloudflare secrets `TOKEN_HMAC_KEY` and `ADMIN_TOKEN`; issue one
  developer token per person without committing any secret.
- Move the program page and completed slice pages into the repository's
  completed-plan archive only after acceptance.

## Acceptance run

- Deploy the Worker from `Tools/CoordinationServer`, verify the health and
  authenticated session routes, and configure two Windows machines on different
  networks.
- Verify viewing presence, pre-edit reservation, simultaneous acquisition,
  remote conflict, cancel, override and displacement notification, clean close,
  process termination, network loss, Worker outage, token revocation, session
  refresh, reconnect, hibernation, and 120-second stale expiry.
- Confirm local dirty work survives pending, failed, offline, and domain-reload
  save paths. Confirm only one authoritative owner exists for each path.
- Confirm no token, session, local settings, logs, cache, or generated lease
  state is tracked by Git.

## Verification and documentation

- Run `npm test`, `npm run docs:build`, backend type checks and tests, the full
  Unity Coordination EditMode suite, and a Play Mode smoke test against the
  canonical scene at execution time.
- Record commands, dates, machine roles, network conditions, observed expiry,
  and any remaining risk in `PP-7`. Do not claim acceptance from a single-machine
  local test.
- Document token setup, reservations, overrides, offline recovery, advisory
  locking limits, and the manual-outage fallback. Keep manual announcements
  required for protected changes.
- Update the ticket Definition of Done, mark the ticket complete only when every
  acceptance item has evidence, and archive the plan pages after the handoff.

**Commit:** `docs(coordination): document release and handoff`

**Completion gate:** The program and `PP-7` are complete only when the two-
machine evidence and all verification outputs are recorded. If deployment or
acceptance is blocked, leave the plan and ticket active with the exact external
blocker instead of claiming completion.
