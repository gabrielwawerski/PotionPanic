# Implementation Plans

This section holds active implementation plans that need more room than a
ticket body but should still stay visible in the VitePress site while the work
is in progress.

Use one page per implementation slice. Keep milestone intent in
[`../project/mvp-scope.md`](../project/mvp-scope.md), keep execution tracking
in [`../board.md`](../board.md) and the related tickets, and move finished or
superseded plans into [`../archive/completed/index.md`](../archive/completed/index.md).

## Active Plans

- [Coordinated File Leasing Remaining Hardening Implementation Plan](./coordinated-file-leasing-remaining-hardening.md)
- [Coordinated Leasing 09: Release Acceptance and Documentation Handoff](./coordinated-file-leasing-09-release-handoff.md)
- [Coordinated Leasing 08: Coordination Window and Lifecycle](./coordinated-file-leasing-08-ui-lifecycle.md)
- [Coordinated Leasing 07: Conflict-Safe Save Guard](./coordinated-file-leasing-07-save-guard.md)
- [Coordinated File Leasing Program](./coordinated-file-leasing-system.md)
- [Coordinated Leasing 06: Scene and Selected-Prefab Tracking](./coordinated-file-leasing-06-asset-tracking.md)
- [Coordinated File Leasing Stabilization Plan](./coordinated-file-leasing-stabilization.md)
- [Coordinated Leasing 02: Developer and Session Authentication](./coordinated-file-leasing-02-authentication.md)
- [Coordinated Leasing 01: Foundations, Configuration, and Protocol](./coordinated-file-leasing-01-foundations.md)
- [Coordinated Leasing 04: Hibernating WebSocket Synchronization](./coordinated-file-leasing-04-websocket-sync.md)
- [Coordinated Leasing 03: Authoritative State and Expiry](./coordinated-file-leasing-03-authoritative-state.md)
- [Coordinated Leasing 05: Unity Authentication and Connection Service](./coordinated-file-leasing-05-unity-connection.md)
## Writing Rules

- Docboard regenerates the Active Plans links from each plan's frontmatter
  title or first heading. Keep those links title-only.
- Use descriptive file names that match the planned change.
- Keep task-by-task execution notes in tickets rather than duplicating them
  here.
- Promote stable guidance into the evergreen docs when it stops being plan
  specific.
