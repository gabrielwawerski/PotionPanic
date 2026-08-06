---
title: 'Coordinated Leasing 01: Foundations, Configuration, and Protocol'
---

# Coordinated Leasing 01: Foundations, Configuration, and Protocol

**Session goal:** Establish the repository, backend, Unity editor test, and
configuration foundations without connecting to a real service.

**Depends on:** None. PP-8 is already resolved; do not recreate or modify it.

**Produces:** A buildable strict-TypeScript Worker package, a Unity configuration
loader and path matcher, the version-1 flat-envelope contract, and CI that runs
backend checks without deployment credentials.

## Files

- Create `coordination.json`.
- Create or modify `.gitignore` with scoped ignores for `.dev.vars`, local
  coordination settings, logs, caches, and generated lease state.
- Modify `Assets/Tests/EditMode/PotionPanic.EditModeTests.asmdef` to reference
  `PotionPanic.Editor`.
- Create `Assets/Scripts/Editor/Coordination/CoordinationConfig.cs`,
  `CoordinationPathMatcher.cs`, `CoordinationProtocol.cs`, and matching tests
  under `Assets/Tests/EditMode/Coordination/`.
- Create `Tools/CoordinationServer/package.json`, `package-lock.json`,
  `tsconfig.json`, `wrangler.jsonc`, `vitest.config.ts`, `.dev.vars.example`,
  `README.md`, `src/index.ts`, `src/env.ts`, `src/protocol.ts`, and focused
  protocol tests.
- Create `.github/workflows/coordination-server.yml`.

## Implementation steps

- Add the committed configuration with `schemaVersion: 1`, project ID
  `potion-panic`, the placeholder Worker base URL, a 30-second heartbeat, and
  the enabled exclusive scene rule from the program page.
- Make the C# loader reject missing or malformed required fields, normalize
  slash direction and Unicode paths, honor a local untracked endpoint override,
  and match `**/` against zero or more directories. Disabled rules must never
  match.
- Define flat protocol envelopes with protocol version `1`, UUID request IDs
  for mutations, canonical path fields, state-version fields, and the message
  names listed in the program page. Reject control characters, traversal,
  drive prefixes, leading separators, oversized messages, and invalid versions.
- Scaffold a strict Worker package with the SQLite Durable Object migration
  binding and Vitest Workers integration. The Worker may return a deliberate
  not-yet-authenticated response in this slice; do not implement auth or state.
- Add CI steps for `npm ci`, type checking, Vitest, and Wrangler dry-run. CI
  must not require `TOKEN_HMAC_KEY`, `ADMIN_TOKEN`, or a deployed endpoint.

## Verification

- Unity Test Runner: run the Coordination EditMode tests and confirm rule
  matching covers the root scene, nested scenes, disabled rules, invalid paths,
  and local override precedence.
- Backend: from `Tools/CoordinationServer`, run `npm ci`, `npm run typecheck`,
  `npm test`, and `npx wrangler deploy --dry-run`.
- Repository: run `npm test` and `npm run docs:build`; both must pass.

**Commit:** `feat(coordination): scaffold backend and configuration`

**Handoff:** Record the commit and all command output in `PP-7`. The next
session may add authentication only if the protocol and configuration tests are
green.
