import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

import {
  createTicketFile,
  fixTickets,
  scanTickets,
  validateTickets,
} from "../../../Docs/.vitepress/lib/markdown-writer-plugin.mjs";

function writeTicket(root, fileName, content) {
  fs.writeFileSync(path.join(root, fileName), content);
}

test(
  "createTicketFile uses the configured section template when no body is provided",
  () => {
    const tempRoot = fs.mkdtempSync(
      path.join(os.tmpdir(), "pp-ticket-create-"));

    const created = createTicketFile(tempRoot, {
      assignee: "Gabriel",
      prefix: "PP",
      priority: "medium",
      sections: ["Description", "Acceptance Criteria", "Notes"],
      status: "backlog",
      tags: [],
      title: "Create templated ticket",
    });

    assert.equal(created.id, 1);
    assert.equal(created.assignee, "Gabriel");
    assert.equal(created.url, "/tickets/PP-1.html");

    const saved = fs.readFileSync(path.join(tempRoot, "PP-1.md"), "utf8");
    assert.match(saved, /^assignee: Gabriel$/m);
    assert.match(saved, /^## Description\s*$/m);
    assert.match(saved, /^## Acceptance Criteria\s*$/m);
    assert.match(saved, /^## Notes\s*$/m);
  });

test("scanTickets returns assignee metadata from frontmatter", () => {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "pp-ticket-scan-"));

  writeTicket(
    tempRoot,
    "PP-7.md",
    [
      "---",
      "id: 7",
      "title: Owned ticket",
      "status: doing",
      "priority: high",
      "assignee: Aga",
      "tags:",
      "  - docs-workflow",
      "---",
      "",
      "## Description",
      "",
      "Assignee should round-trip.",
    ].join("\n")
  );

  const [ticket] = scanTickets(tempRoot, "tickets");

  assert.equal(ticket.assignee, "Aga");
  assert.equal(ticket.title, "Owned ticket");
});

test(
  "validateTickets reports both identity issues and missing required sections",
  () => {
    const tempRoot = fs.mkdtempSync(
      path.join(os.tmpdir(), "pp-ticket-validate-"));

    writeTicket(
      tempRoot,
      "wrong-slug.md",
      [
        "---",
        "id: 2",
        "title: Needs structure",
        "status: todo",
        "priority: medium",
        "tags: []",
        "---",
        "",
        "## Description",
        "",
        "Only one section exists.",
      ].join("\n")
    );

    const issues = validateTickets(tempRoot, "tickets", "PP", [
      "Description",
      "Acceptance Criteria",
      "Notes",
    ]);

    assert.equal(issues.length, 2);
    assert.deepEqual(
      issues.map((issue) => issue.type).sort(),
      ["identity", "missing-sections"]
    );

    const identityIssue = issues.find((issue) => issue.type === "identity");
    assert.equal(identityIssue.currentSlug, "wrong-slug");
    assert.equal(identityIssue.fixedSlug, "PP-2");

    const sectionIssue = issues.find(
      (issue) => issue.type === "missing-sections");
    assert.deepEqual(sectionIssue.missingSections, [
      "Acceptance Criteria",
      "Notes",
    ]);
  });

test("fixTickets repairs slug and inserts any missing required sections",
  () => {
    const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "pp-ticket-fix-"));

    writeTicket(
      tempRoot,
      "ticket.md",
      [
        "---",
        "id: 4",
        "title: Needs fixing",
        "status: doing",
        "priority: high",
        "tags: []",
        "---",
        "",
        "## Description",
        "",
        "Still missing structure.",
      ].join("\n")
    );

    const fixedIssues = fixTickets(tempRoot, "tickets", "PP", [
      "Description",
      "Acceptance Criteria",
      "Definition of Done",
    ]);

    assert.equal(fixedIssues.length, 2);
    assert.equal(fs.existsSync(path.join(tempRoot, "ticket.md")), false);
    assert.equal(fs.existsSync(path.join(tempRoot, "PP-4.md")), true);

    const repaired = fs.readFileSync(path.join(tempRoot, "PP-4.md"), "utf8");
    assert.match(repaired, /^id: 4$/m);
    assert.match(repaired, /^## Acceptance Criteria\s*$/m);
    assert.match(repaired, /^## Definition of Done\s*$/m);
  });
