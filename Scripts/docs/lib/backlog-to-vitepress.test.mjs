import test from "node:test";
import assert from "node:assert/strict";

import {
  buildAssigneeFollowUpTicket,
  buildTicketFilename,
  convertBacklogTask,
  mapBacklogStatus,
  parseTaskIdNumber,
} from "./backlog-to-vitepress.mjs";

test("parseTaskIdNumber keeps decimal task ids intact", () => {
  assert.equal(parseTaskIdNumber("PP-1.4"), 1.4);
  assert.equal(parseTaskIdNumber("PP-4"), 4);
});

test("mapBacklogStatus converts board labels into vitepress-friendly keys",
  () => {
    assert.equal(mapBacklogStatus("Backlog"), "backlog");
    assert.equal(mapBacklogStatus("To Do"), "todo");
    assert.equal(mapBacklogStatus("Doing"), "doing");
    assert.equal(mapBacklogStatus("Test / Review"), "review");
    assert.equal(mapBacklogStatus("Done"), "done");
  });

test(
  "convertBacklogTask promotes workflow metadata into structured frontmatter",
  () => {
    const raw = `---
id: PP-3
title: Add Milestone 1 CharacterController movement
status: To Do
priority: medium
labels: []
milestone: m-0
dependencies:
  - PP-2
documentation:
  - README.md
modified_files:
  - Assets/Scripts/Runtime
---

## Description

Keep the current milestone movement work scoped.
`;

    const converted = convertBacklogTask(raw);

    assert.deepEqual(converted.frontmatter, {
      affectedFiles: ["Assets/Scripts/Runtime"],
      dependencies: ["PP-2"],
      documentation: ["README.md"],
      id: 3,
      milestone: "m-0",
      priority: "medium",
      status: "todo",
      tags: [],
      title: "Add Milestone 1 CharacterController movement",
    });
    assert.match(converted.body, /## Description/);
    assert.doesNotMatch(converted.body, /## Legacy Notes/);
  });

test(
  "buildAssigneeFollowUpTicket creates the deferred assignee enhancement ticket",
  () => {
    const followUp = buildAssigneeFollowUpTicket(5);

    assert.equal(buildTicketFilename("PP", followUp.frontmatter.id), "PP-5.md");
    assert.equal(followUp.frontmatter.status, "backlog");
    assert.equal(followUp.frontmatter.title,
      "Add assignee support to the VitePress board");
    assert.match(followUp.body, /assignee/i);
  });
