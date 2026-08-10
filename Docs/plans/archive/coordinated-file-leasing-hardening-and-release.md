# Coordinated File Leasing Hardening and Release Plan

## Summary

Status on 2026-08-08: **Deployment complete; external release acceptance
deferred.** Production secrets, the deployed endpoint, health, and Machine A
authentication are verified in `PP-7`. Machine B and every two-machine
acceptance row remain incomplete.

The original pre-deployment review found the architecture appropriate for a
small Unity team, with these automated gates passing at that checkpoint:

- Worker typecheck, security audit, deployment dry run, and 77/77 tests.
- Unity 6000.5.1f1 Coordination EditMode suite: 140/140.
- Documentation tests: 11/11; VitePress build passes.
- Clean `master` at `d9792d7`, matching `origin/master`.

Slice 09 remains incomplete because no live two-machine evidence exists. The
earlier placeholder endpoint and Wrangler-authentication blockers are resolved.
The current editor UI also requires manual asset-path entry for lease actions,
renders state rows without interactions, and cannot manually cancel a
reservation. Those UX and protocol gaps require separate approval and do not
count as completed acceptance behavior.

## Required Hardening

- Restrict the Worker to `projectId === "potion-panic"` before calling
  `idFromName`. Return 404 for every other project so unauthenticated callers
  cannot create unlimited Durable Objects by varying the URL.
- Replace monolithic snapshots with bounded chunks:
    - Each envelope remains at most 16 KiB.
    - Snapshot chunks carry `snapshotId`, zero-based `chunkIndex`, `chunkCount`,
      one shared `stateVersion`, and partial presence/lease arrays.
    - Unity buffers at most 256 KiB and applies the snapshot atomically after
      every chunk arrives.
    - Reject state-growing mutations with correlated `state_capacity_exceeded`
      when the 256 KiB project limit would be exceeded.
- Make authentication constant-time per request:
    - Store an indexed SHA-256 token lookup value alongside the existing HMAC
      digest.
    - Query one developer or session row, then verify the HMAC digest.
    - Keep at most eight valid sessions per developer; evict the oldest
      unconnected session first and return HTTP 429 when all eight belong to
      active connections.
- On developer revocation, delete that developer’s reservations as well as
  sessions, presence, editing leases, and connections. Broadcast the resulting
  reservation releases.
- Define identical cross-runtime path canonicalization: NFC normalization, slash
  normalization, and ASCII `A-Z` folding only. Add shared Unicode vectors
  including `İ`, `Ä`, composed/decomposed characters, and mixed-case ASCII.
- Drain all outstanding client request handles exactly once when a socket closes
  and raise `RequestSendFailed` for each. This prevents stale acquisition
  tracking and reconnect-time memory growth.
- Limit task context to 256 UTF-16 code units in settings parsing and the editor
  UI. Apply the same defensive limit to task and Git branch metadata before
  serialization so an oversized value cannot silently prevent saving.
- Add a successful-credential callback so saving the token immediately
  reconnects the existing service.
- Update `.dev.vars.example`, server documentation, and tests to list
  `TOKEN_HMAC_KEY` and `ADMIN_TOKEN`.
- Modernize `wrangler.jsonc` before the first deployment:
    - Use the current declarative `exports` entry for the SQLite-backed Durable
      Object.
    - Declare both secrets as required.
    - Set `workers_dev: true`, disable preview URLs, and enable full
      observability for the initial low-volume release.
    - Keep the existing verification-only GitHub workflow; production deployment
      remains manual.

## Cloudflare and Developer Setup

1. Run all local gates from `../../Tools/CoordinationServer`: `npm ci`,
   typecheck, tests, audit, and Wrangler dry run.
2. Run `npx wrangler login`, complete browser authorization, then run
   `npx wrangler whoami`. Record the selected account ID in Wrangler
   configuration if the login exposes multiple accounts.
3. Confirm or create the account’s `workers.dev` subdomain. The selected
   endpoint will be
   `https://potion-panic-coordination.<account-subdomain>.workers.dev`.
4. Generate separate 256-bit URL-safe values for `TOKEN_HMAC_KEY` and
   `ADMIN_TOKEN`. Store both in the team password manager. Never place
   production values in Git, command history, tickets, or Unity settings.
5. Deploy atomically with
   `npx wrangler deploy --strict --secrets-file <temporary-secret-file>`, then
   remove the temporary file. Confirm the deployment output lists the
   `COORDINATION_OBJECT` binding and SQLite class.
6. Run `npx wrangler secret list`, `npx wrangler deployments list`, and
   `Invoke-RestMethod <worker-url>/health`. Require the expected service name, a
   parseable server time, and HTTP 200.
7. Replace the placeholder `serverBaseUrl`
   in [coordination.json](C:/Dev/PotionPanic/coordination.json:4) with the exact
   deployed HTTPS URL. Run the full gates again and commit this configuration
   with the release documentation.
8. Issue one developer token per person using `scripts/issue-token.mjs`. Load
   `ADMIN_TOKEN` into the process environment without embedding it in shell
   history, clear it immediately afterward, and transmit each one-time token
   through an approved secret channel.
9. On each Windows machine:
    - Open the project in Unity 6000.5.1f1.
    - Enter the assigned token in the credential prompt.
    - Confirm automatic connection in `Window > Potion Panic > Coordination`.
    - Set a task context of at most 256 characters.
    - Verify the displayed developer identity, Git branch, `Connected` state,
      and empty initial warnings.
10. For local-only backend work, create the ignored `.dev.vars` with separate
    development secrets, run `npx wrangler dev --local`, and set
    `UserSettings/PotionPanic/coordination.local.json` to
    `http://127.0.0.1:8787`. Never reuse production secrets locally.
11. Monitor Worker errors, 401/429 responses, Durable Object requests, rows
    written, and storage in Cloudflare. SQLite-backed Durable Objects are
    available on the Free plan, subject to daily
    limits. [Cloudflare pricing](https://developers.cloudflare.com/durable-objects/platform/pricing/)
12. Rotate `ADMIN_TOKEN` independently after suspected exposure. Rotating
    `TOKEN_HMAC_KEY` invalidates every developer and session token, so revoke
    old developer records and issue replacements.

## Verification and Release Acceptance

- Add automated coverage for arbitrary-project rejection without Durable Object
  creation, snapshots over 16 KiB, incomplete/out-of-order chunks, the 256 KiB
  limit, indexed token authentication, session churn, reservation removal on
  revocation, Unicode canonicalization, socket-close request draining, long task
  contexts, and credential-triggered reconnect.
- Re-run backend typecheck, 77+ Worker tests,
  `npm audit --audit-level=moderate`, Wrangler dry run, root tests,
  documentation build, the complete Unity Coordination suite, and Play Mode
  smoke testing.
- Use two clean disposable clones on separate Windows machines and different
  networks. Verify presence, reservation, simultaneous acquisition, denial,
  cancel, explicit override, displacement notification, normal close, abrupt
  termination, network loss, reconnect, 120-second stale expiry, and the
  two-confirmation uncoordinated-save path.
- Temporarily disable the `workers.dev` route to exercise a real endpoint
  outage, confirm local dirty work remains recoverable, then restore the route
  and verify authoritative resynchronization.
- Leave one client connected across the 24-hour session expiry and record
  automatic session recreation. Leave both clients idle long enough to exercise
  hibernation restoration, then perform another lease mutation.
- Revoke one developer through the administrative endpoint. Require immediate
  `AuthenticationFailed`, socket closure, removal of that developer’s presence,
  leases, and reservations, and successful coordination by the remaining
  developer.
- Record commands, deployment/version IDs, machine roles, networks, observed
  expiry timings, and results in `PP-7`. Update the evergreen onboarding,
  workflow, and editor-safety documentation. Archive the leasing plans and close
  PP-7 only after every acceptance item has evidence.

## Assumptions

- `workers.dev` is accepted for this internal, advisory first release.
  Cloudflare recommends a custom domain or Worker route for business-critical
  production services, so migrate if this becomes operationally
  critical. [Cloudflare workers.dev guidance](https://developers.cloudflare.com/workers/configuration/routing/workers-dev/)
- Manual Wrangler deployment remains the production path; GitHub Actions
  continues to verify only.
- The first deployment may replace the unreleased protocol contract without
  backward compatibility.
- Cloudflare’s declarative Durable Object `exports` format is used because the
  Worker has not completed production release. Legacy migrations remain
  supported, but `exports` is the current source-of-truth
  model. [Durable Object exports](https://developers.cloudflare.com/durable-objects/reference/durable-objects-migrations/)
- Production secrets use Cloudflare secret bindings and are never
  tracked. [Cloudflare Workers secrets](https://developers.cloudflare.com/workers/configuration/secrets/)
