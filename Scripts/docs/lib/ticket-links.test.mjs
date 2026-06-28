import assert from "node:assert/strict";
import test from "node:test";

import {
  buildDocumentationHref,
  buildTicketHref,
} from "../../../Docs/.vitepress/lib/ticket-links.mjs";

test("buildTicketHref maps dependency references to ticket pages", () => {
  assert.equal(buildTicketHref("PP-2"), "/tickets/PP-2.html");
  assert.equal(buildTicketHref("tickets/PP-3"), "/tickets/PP-3.html");
});

test("buildDocumentationHref links docs pages that exist in the VitePress site",
  () => {
    assert.equal(buildDocumentationHref("../project/mvp-scope.md"),
      "/project/mvp-scope.html");
    assert.equal(buildDocumentationHref("README.md"), "/README.html");
    assert.equal(buildDocumentationHref("Docs/collaboration/team-workflow.md"),
      "/collaboration/team-workflow.html");
  });

test("buildDocumentationHref leaves non-site markdown files unlinked", () => {
  assert.equal(buildDocumentationHref("AGENTS.md"), null);
  assert.equal(buildDocumentationHref("Assets/Scenes/Laboratory.unity"), null);
});
