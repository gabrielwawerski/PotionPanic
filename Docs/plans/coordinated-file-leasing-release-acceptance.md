---
title: Coordinated File Leasing Release Acceptance
status: active
supersedes:
  - coordinated-file-leasing-remaining-hardening.md
---

# Coordinated File Leasing Release Acceptance

## Summary

The coordination Worker is deployed at the URL in `coordination.json`, and the
latest PP-7 evidence records successful `/health` checks for that endpoint.
PP-7 remains open because final release acceptance still needs interactive
Unity smoke evidence, credentialed Wrangler-tail evidence, and two Windows
machines on different networks completing the acceptance matrix.

This page is the active release-acceptance plan. The detailed hardening
implementation history is archived in
[`archive/coordinated-file-leasing-remaining-hardening.md`](archive/coordinated-file-leasing-remaining-hardening.md).

## Current State

- Configured endpoint:
  `https://potion-panic-coordination.gabriel-wawerski.workers.dev`.
- Worker deployment, required secret names, and unauthenticated `/health`
  checks are recorded in [`../tickets/PP-7.md`](../tickets/PP-7.md).
- Machine A provisioning and connection have partial evidence in PP-7.
- An operator-reported two-machine run exercised save denial, the non-saving
  Cancel save and Keep working choices, lease override, and the displaced
  client's next save conflict. The evidence lacks the machine roles, network
  conditions, and timestamps required for a completed acceptance-matrix row.
- Remaining work includes the complete documented two-machine matrix,
  credentialed tail evidence, and a current interactive single-machine smoke.
- Do not mark PP-7 complete, archive the program plan, or describe release
  acceptance as complete until the remaining evidence is recorded.

## Remaining Acceptance Work

1. Re-run local gates from the current checkout:
   `npm test`, `npm run docs:build`, `Tools/CoordinationServer` typecheck,
   Worker tests, `npm audit --audit-level=high`, and `npx wrangler deploy --dry-run`.
2. Verify the configured endpoint with `GET /health` and record the timestamp
   and response shape in PP-7 without saving credentials or secrets.
3. Complete an interactive single-machine Unity smoke with the current client:
   authenticate through the Coordination window, connect, acquire or release a
   claim against the canonical scene, and record filtered evidence.
4. Capture filtered `npx wrangler tail --format json` evidence during at least
   one credentialed connection or lease operation. Exclude authorization
   headers, developer tokens, session tokens, secrets, Credential Manager
   contents, and raw local settings.
5. Record the machine roles, different-network conditions, timestamps, Unity
   versions, and redacted screenshots or logs for the observed denial, cancel,
   keep-working, override, and displacement flow. Then run the remaining
   two-machine matrix: presence, reservation, reservation cancellation,
   simultaneous acquire, clean close, abrupt termination with 120-150 second
   expiry, outage fallback, reconnect, hibernation restoration, 24-hour session
   recreation, and revocation.
6. Append dated commands, versions, machine roles, network conditions, observed
   timings, and failures to PP-7.
7. Close PP-7 only after every acceptance row and the definition of done have
   evidence.

## Boundaries

- Production deployment remains a manual authenticated operator action.
- GitHub Actions remains verification-only for the coordination server.
- Do not change `coordination.json` unless a future deployment returns a
  different verified Worker URL.
- Do not commit tokens, sessions, `.dev.vars`, local settings, logs, cache
  output, or generated lease state.
