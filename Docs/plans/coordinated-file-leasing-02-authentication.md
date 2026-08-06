---
title: 'Coordinated Leasing 02: Developer and Session Authentication'
---

# Coordinated Leasing 02: Developer and Session Authentication

**Session goal:** Add revocable developer tokens and opaque 24-hour sessions
without exposing secrets in URLs, logs, or durable plaintext storage.

**Depends on:** Slice 01.

**Produces:** Tested token issuance, authenticated session creation, separate
administrator authentication, and revocation primitives for Slice 03.

## Files

- Modify `Tools/CoordinationServer/src/index.ts` and `src/env.ts`.
- Create or modify `Tools/CoordinationServer/src/auth/crypto.ts`,
  `src/auth/session.ts`, and `src/auth/admin.ts`.
- Create `Tools/CoordinationServer/scripts/issue-token.mjs`.
- Add authentication tests under `Tools/CoordinationServer/tests/auth/`.
- Update `Tools/CoordinationServer/README.md` with issuance, rotation, and
  revocation procedures.

## Implementation steps

- Generate developer tokens from 32 random bytes and persist only a
  domain-separated HMAC-SHA-256 digest with developer ID and display name.
- Hash opaque session tokens with a separate domain separator, developer ID,
  and 24-hour expiry. Return the canonical developer ID, display name, server
  time, and session expiry from `POST /v1/projects/{projectId}/sessions`.
- Require the bearer developer token on session creation and the bearer session
  token on later authenticated routes. Derive project and developer identity
  from the route and stored session, never from client JSON.
- Protect administrative issuance and revocation with `ADMIN_TOKEN` and keep it
  independent from developer authentication. Revocation deletes active sessions
  and exposes the affected developer ID to the socket layer without affecting
  other developers.
- Ensure token values never appear in request URLs, structured logs, thrown
  errors, or test snapshots. The issuance script prints a token once and never
  writes it to disk.

## Verification

Run from `Tools/CoordinationServer`:

- `npm run typecheck`
- `npm test -- tests/auth`
- `npm test`

The focused tests must cover random token generation, digest-only persistence,
expiry, invalid tokens, project mismatch, revocation, administrator rejection,
and secret redaction. Confirm the Worker dry-run still succeeds.

**Commit:** `feat(coordination): add revocable opaque sessions`

**Handoff:** Record the commit and test evidence in `PP-7`. Slice 03 may use the
authenticated developer/session context but must own all lease state transitions.
