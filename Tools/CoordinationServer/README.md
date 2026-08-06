# Potion Panic Coordination Server

Cloudflare Worker and SQLite-backed Durable Object foundation for advisory
editing coordination. Slice 01 provides the version-1 protocol contract, a
Durable Object migration binding, and the unauthenticated health endpoint.

## Commands

Run these commands from this directory:

```powershell
npm ci
npm run typecheck
npm test
npx wrangler deploy --dry-run
```

`GET /health` returns only the service identifier and current server time.
Authentication, developer management, state transitions, and WebSocket
synchronization are intentionally deferred to later slices. The remaining
routes return HTTP 501 in this slice.

Copy `.dev.vars.example` to `.dev.vars` only when a later slice requires local
secrets. Never commit `.dev.vars`.
