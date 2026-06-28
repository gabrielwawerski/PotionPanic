import assert from "node:assert/strict";
import test from "node:test";

import {
  COMPACT_TICKET_DETAIL_BREAKPOINT,
  isTicketDetailCompactViewport,
  shouldCollapseTicketDetailMetadataByDefault,
} from "../../../Docs/.vitepress/lib/ticket-detail-layout.mjs";

test("isTicketDetailCompactViewport uses the modal compact breakpoint", () => {
  assert.equal(COMPACT_TICKET_DETAIL_BREAKPOINT, 959);
  assert.equal(isTicketDetailCompactViewport(480), true);
  assert.equal(isTicketDetailCompactViewport(820), true);
  assert.equal(isTicketDetailCompactViewport(959), true);
  assert.equal(isTicketDetailCompactViewport(960), false);
  assert.equal(isTicketDetailCompactViewport(1280), false);
});

test("shouldCollapseTicketDetailMetadataByDefault collapses on compact widths", () => {
  assert.equal(shouldCollapseTicketDetailMetadataByDefault(480), true);
  assert.equal(shouldCollapseTicketDetailMetadataByDefault(820), true);
  assert.equal(shouldCollapseTicketDetailMetadataByDefault(960), false);
});
