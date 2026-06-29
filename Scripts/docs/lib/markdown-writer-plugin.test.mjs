import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

import {
  archiveTicketFile,
  createTicketFile,
  fixTickets,
  restoreTicketFile,
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
      affectedFiles: ["Assets/Scenes/Laboratory.unity"],
      dependencies: ["PP-2"],
      documentation: ["README.md", "project/mvp-scope.md"],
      milestone: "m-0",
      prefix: "PP",
      priority: "medium",
      sections: ["Description", "Acceptance Criteria", "Notes"],
      status: "backlog",
      tags: [],
      title: "Create templated ticket",
    });

    assert.equal(created.id, 1);
    assert.equal(created.assignee, "Gabriel");
    assert.deepEqual(created.dependencies, ["PP-2"]);
    assert.equal(created.milestone, "m-0");
    assert.equal(created.url, "/tickets/PP-1.html");

    const saved = fs.readFileSync(path.join(tempRoot, "PP-1.md"), "utf8");
    assert.match(saved, /^assignee: Gabriel$/m);
    assert.match(saved, /^milestone: m-0$/m);
    assert.match(saved, /^dependencies:\r?$/m);
    assert.match(saved, /^  - PP-2$/m);
    assert.match(saved, /^documentation:\r?$/m);
    assert.match(saved, /^  - README.md$/m);
    assert.match(saved, /^affectedFiles:\r?$/m);
    assert.match(saved, /^  - Assets\/Scenes\/Laboratory\.unity$/m);
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
      "milestone: m-0",
      "dependencies:",
      "  - PP-2",
      "  - PP-4",
      "documentation:",
      "  - README.md",
      "affectedFiles:",
      "  - Assets/Scenes/Laboratory.unity",
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
  assert.equal(ticket.milestone, "m-0");
  assert.deepEqual(ticket.dependencies, ["PP-2", "PP-4"]);
  assert.deepEqual(ticket.documentation, ["README.md"]);
  assert.deepEqual(ticket.affectedFiles, ["Assets/Scenes/Laboratory.unity"]);
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

test("archiveTicketFile moves a ticket into the archive directory and stamps archivedAt",
  () => {
    const tempRoot = fs.mkdtempSync(
      path.join(os.tmpdir(), "pp-ticket-archive-"));
    const activeDir = path.join(tempRoot, "tickets");
    const archiveDir = path.join(tempRoot, "archive", "tickets");

    fs.mkdirSync(activeDir, {recursive: true});
    writeTicket(
      activeDir,
      "PP-8.md",
      [
        "---",
        "id: 8",
        "title: Archive me",
        "status: review",
        "priority: high",
        "assignee: Aga",
        "milestone: m-0",
        "dependencies:",
        "  - PP-2",
        "documentation:",
        "  - README.md",
        "affectedFiles:",
        "  - Docs/board.md",
        "tags:",
        "  - docs",
        "---",
        "",
        "## Description",
        "",
        "Archive this ticket without losing metadata.",
      ].join("\n")
    );

    const archived = archiveTicketFile(tempRoot, {
      targetDir: "archive/tickets",
      url: "/tickets/PP-8.html",
    });

    assert.equal(fs.existsSync(path.join(activeDir, "PP-8.md")), false);
    assert.equal(fs.existsSync(path.join(archiveDir, "PP-8.md")), true);
    assert.equal(archived.url, "/archive/tickets/PP-8.html");
    assert.equal(archived.status, "review");
    assert.equal(archived.assignee, "Aga");
    assert.deepEqual(archived.dependencies, ["PP-2"]);
    assert.ok(archived.archivedAt);

    const archivedFile = fs.readFileSync(
      path.join(archiveDir, "PP-8.md"),
      "utf8"
    );
    assert.match(archivedFile, /^archivedAt: .+$/m);
  });

test("restoreTicketFile moves an archived ticket back to the active directory and clears archivedAt",
  () => {
    const tempRoot = fs.mkdtempSync(
      path.join(os.tmpdir(), "pp-ticket-restore-"));
    const archiveDir = path.join(tempRoot, "archive", "tickets");
    const activeDir = path.join(tempRoot, "tickets");

    fs.mkdirSync(archiveDir, {recursive: true});
    writeTicket(
      archiveDir,
      "PP-8.md",
      [
        "---",
        "id: 8",
        "title: Restore me",
        "status: review",
        "priority: high",
        "archivedAt: 2026-06-29T12:00:00.000Z",
        "tags:",
        "  - docs",
        "---",
        "",
        "## Description",
        "",
        "Restore this ticket to the active board.",
      ].join("\n")
    );

    const restored = restoreTicketFile(tempRoot, {
      targetDir: "tickets",
      url: "/archive/tickets/PP-8.html",
    });

    assert.equal(fs.existsSync(path.join(archiveDir, "PP-8.md")), false);
    assert.equal(fs.existsSync(path.join(activeDir, "PP-8.md")), true);
    assert.equal(restored.url, "/tickets/PP-8.html");
    assert.equal(restored.status, "review");
    assert.equal(restored.archivedAt, "");

    const restoredFile = fs.readFileSync(
      path.join(activeDir, "PP-8.md"),
      "utf8"
    );
    assert.doesNotMatch(restoredFile, /^archivedAt: .+$/m);
  });
