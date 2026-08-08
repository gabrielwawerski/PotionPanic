---
title: 'Coordinated Leasing 09: Release Acceptance and Documentation Handoff'
---

# Coordinated Leasing 09: Release Acceptance and Documentation Handoff

**Session goal:** Deploy the backend, prove the full two-machine behavior, then
publish stable operating guidance and close the program.

**Depends on:** Slices 01 through 08.

**Produces:** A release-ready coordination system with evidence recorded in
`PP-7`, updated evergreen docs, and an archived program plan.

**Current step:** Implement the 2026-08-08 hardening review before deployment.

## Pre-deployment hardening

- Reject every project identifier except `potion-panic` before resolving a
  Durable Object namespace ID.
- Split snapshots into envelopes no larger than 16 KiB. Each chunk carries a
  snapshot ID, zero-based index, chunk count, and a shared state version. The
  Unity client may buffer at most 256 KiB and applies a snapshot only after all
  chunks arrive.
- Reject state-growing requests with correlated `state_capacity_exceeded` when
  the resulting project snapshot would exceed 256 KiB.
- Authenticate developer and session tokens through indexed SHA-256 lookup
  values, then verify the existing HMAC digest. Keep at most eight valid
  sessions per developer, evict the oldest disconnected session first, and
  return HTTP 429 if all eight sessions have active connections.
- Remove and broadcast a revoked developer's reservations with that
  developer's sessions, presence, editing leases, and connections.
- Use identical path canonicalization in TypeScript and C#: NFC normalization,
  slash normalization, and ASCII `A-Z` folding only. Cover non-ASCII and
  composed/decomposed Unicode vectors.
- Drain pending Unity request handles exactly once when a socket closes. Limit
  task context and serialized Git/task metadata to 256 UTF-16 code units, and
  reconnect immediately after a credential is saved successfully.
- Declare the Worker Durable Object through Wrangler's declarative `exports`,
  require `TOKEN_HMAC_KEY` and `ADMIN_TOKEN`, enable `workers.dev`, disable
  preview URLs, and enable full observability for the initial release. Keep
  production deployment manual.

Implementation must add regression coverage for each item and leave the live
endpoint unchanged until Wrangler authentication and an actual deployment
produce the exact `workers.dev` URL.

## Files and external state

- Modify `coordination.json` to replace the placeholder endpoint after the
  Worker is deployed.
- Update `README.md`, [Docs/onboarding/getting-started.md](../onboarding/getting-started.md),
  [Docs/collaboration/team-workflow.md](../collaboration/team-workflow.md), and [Docs/guides/unity/editor-safety.md](../guides/unity/editor-safety.md).
- Append deployment, test, and two-machine evidence to [Docs/tickets/PP-7.md](../tickets/PP-7.md).
- Create Cloudflare secrets `TOKEN_HMAC_KEY` and `ADMIN_TOKEN`; issue one
  developer token per person without committing any secret.
- Move the program page and completed slice pages into
  `Docs/archive/completed/` only after acceptance, and update
  [Docs/archive/completed/index.md](../archive/completed/index.md) through the existing plan archive flow.

## Acceptance run

- Deploy the Worker from `Tools/CoordinationServer`, verify the defined
  unauthenticated `GET /health` response and authenticated session route, and
  configure two Windows machines on different networks.
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
