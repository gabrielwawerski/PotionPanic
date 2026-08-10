---
title: 'Coordinated Leasing 02: Developer and Session Authentication'
---

# Coordinated Leasing 02: Developer and Session Authentication

**Session goal:** Add revocable developer tokens and opaque 24-hour sessions without exposing secrets in URLs, logs, or durable plaintext storage.

**Depends on:** Slice 01.

**Produces:** The auth-only Durable Object foundation, tested token issuance, authenticated session creation, separate administrator authentication, and revocation state for Slices 03 and 04.

## Files

- Modify `Tools/CoordinationServer/src/index.ts` and `src/env.ts`.
- Modify `Tools/CoordinationServer/src/coordination-object.ts` to add only the developer and session persistence required by this slice.
- Create or modify `Tools/CoordinationServer/src/auth/crypto.ts`,
  `src/auth/session.ts`, and `src/auth/admin.ts`.
- Create `Tools/CoordinationServer/scripts/issue-token.mjs`.
- Add authentication tests under `Tools/CoordinationServer/tests/auth/`.
- Update `Tools/CoordinationServer/README.md` with issuance, rotation, and revocation procedures.

## Implementation steps

- Generate developer tokens from 32 random bytes and persist only a domain-separated HMAC-SHA-256 digest with developer ID and display name in the Durable Object's `developers` table. Create the `sessions` table and the initial state-version row in that same object; Slice 03 extends this schema and must not recreate any of them.
- Hash opaque session tokens with a separate domain separator, developer ID, and 24-hour expiry. Return the canonical developer ID, display name, server time, lease and reservation TTLs, and state version from
  `POST /v1/projects/{projectId}/sessions`. Do not create or return a
  `connectionId` in this HTTP route.
- Require the bearer developer token on session creation and the bearer session token on later authenticated routes. Derive project and developer identity from the route and stored session, never from client JSON.
- Implement `POST /v1/projects/{projectId}/developers` and
  `DELETE /v1/projects/{projectId}/developers/{developerId}` exactly as defined in the program contract. Protect them with `ADMIN_TOKEN`, keep that secret independent from developer authentication, and make `scripts/issue-token.mjs`
  call the create route. Revocation marks the developer revoked and deletes only that developer's sessions. Slice 04 closes the revoked developer's live sockets when it consumes this revocation state.
- Ensure token values never appear in request URLs, structured logs, thrown errors, or test snapshots. The issuance script prints a token once and never writes it to disk.

## Verification

Run from `Tools/CoordinationServer`:

- `npm run typecheck`
- `npm test -- tests/auth`
- `npm test`

The focused tests must cover random token generation, digest-only persistence, expiry, invalid tokens, project mismatch, revocation, administrator rejection, secret redaction, the auth-table schema, the create-developer response, and session creation without a connection ID. Confirm the Worker dry-run still succeeds.

**Commit:** `feat(coordination): add revocable opaque sessions`

**Handoff:** Record the commit and test evidence in `PP-7`. Slice 03 may use the authenticated developer/session context but must own all lease state transitions. Slice 04 must close live sockets for a revoked developer; Slice 02 has no socket registry and must not emulate one.
