import assert from "node:assert/strict";
import test from "node:test";

import {
  buildTicketTemplate,
  ensureTicketSections,
  findMissingTicketSections,
  parseTicketSections,
} from "../../../Docs/.vitepress/lib/ticket-sections.mjs";

test("buildTicketTemplate scaffolds headings in the configured order", () => {
  const template = buildTicketTemplate([
    "Description",
    "Acceptance Criteria",
    "Notes",
  ]);

  assert.match(template, /^## Description\s*$/m);
  assert.match(template, /^## Acceptance Criteria\s*$/m);
  assert.match(template, /^## Notes\s*$/m);
  assert.ok(
    template.indexOf("## Description") <
    template.indexOf("## Acceptance Criteria")
  );
  assert.ok(
    template.indexOf("## Acceptance Criteria") < template.indexOf("## Notes")
  );
});

test("parseTicketSections returns ordered configured sections and marks gaps",
  () => {
    const body = [
      "## Description",
      "",
      "Ship a section-aware modal.",
      "",
      "## Notes",
      "",
      "Archived tasks already use this structure.",
    ].join("\n");

    const sections = parseTicketSections(body, [
      "Description",
      "Acceptance Criteria",
      "Notes",
    ]);

    assert.deepEqual(
      sections.map(({ heading, missing }) => ({ heading, missing })),
      [
        { heading: "Description", missing: false },
        { heading: "Acceptance Criteria", missing: true },
        { heading: "Notes", missing: false },
      ]
    );
    assert.equal(sections[0].content, "Ship a section-aware modal.");
    assert.equal(sections[1].content, "");
    assert.equal(sections[2].content,
      "Archived tasks already use this structure.");
  });

test(
  "ensureTicketSections appends missing required headings without removing content",
  () => {
    const updated = ensureTicketSections(
      [
        "## Description",
        "",
        "Existing details stay intact.",
      ].join("\n"),
      ["Description", "Acceptance Criteria", "Definition of Done"]
    );

    assert.equal(
      findMissingTicketSections(updated, [
        "Description",
        "Acceptance Criteria",
        "Definition of Done",
      ]).length,
      0
    );
    assert.match(updated, /Existing details stay intact\./);
    assert.match(updated, /^## Acceptance Criteria\s*$/m);
    assert.match(updated, /^## Definition of Done\s*$/m);
  });
