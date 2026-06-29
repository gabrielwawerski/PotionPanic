import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

async function loadPlanWriterModule() {
  try {
    return await import("../../../Docs/.vitepress/lib/plan-writer.mjs");
  } catch (error) {
    assert.fail(`plan writer module missing: ${error.message}`);
  }
}

function writeMarkdown(root, relativePath, content) {
  const filePath = path.join(root, relativePath);
  fs.mkdirSync(path.dirname(filePath), {recursive: true});
  fs.writeFileSync(filePath, content);
}

test("createPlanFile writes dated frontmatter and adds the plan to the active index",
  async () => {
    const {createPlanFile} = await loadPlanWriterModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-plan-writer-"));

    writeMarkdown(
      docsDir,
      "plans/index.md",
      [
        "# Implementation Plans",
        "",
        "## Active Plans",
        "",
        "_No active plans yet._",
      ].join("\n")
    );

    const created = createPlanFile(docsDir, {
      body: "## Summary\n\nFresh plan body.\n",
      title: "Fresh Plan",
      today: "2026-06-29",
    });

    assert.equal(created.url, "/plans/fresh-plan");
    assert.equal(created.date, "2026-06-29");

    const saved = fs.readFileSync(
      path.join(docsDir, "plans", "fresh-plan.md"),
      "utf8"
    );
    assert.match(saved, /^date: 2026-06-29$/m);
    assert.match(saved, /^# Fresh Plan$/m);
    assert.match(saved, /^## Summary$/m);

    const indexContent = fs.readFileSync(
      path.join(docsDir, "plans", "index.md"),
      "utf8"
    );
    assert.match(indexContent, /- \[Fresh Plan\]\(\.\/fresh-plan\.md\)/);
    assert.doesNotMatch(indexContent, /_No active plans yet\._/);
  });

test("updatePlanFile preserves the existing date and updates the active index label",
  async () => {
    const {updatePlanFile} = await loadPlanWriterModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-plan-writer-"));

    writeMarkdown(
      docsDir,
      "plans/fresh-plan.md",
      [
        "---",
        "date: 2026-06-29",
        "---",
        "",
        "# Fresh Plan",
        "",
        "## Summary",
        "",
        "Old body.",
      ].join("\n")
    );
    writeMarkdown(
      docsDir,
      "plans/index.md",
      [
        "# Implementation Plans",
        "",
        "## Active Plans",
        "",
        "- [Fresh Plan](./fresh-plan.md)",
      ].join("\n")
    );

    const updated = updatePlanFile(docsDir, {
      body: "## Summary\n\nUpdated body.\n",
      title: "Renamed Plan",
      url: "/plans/fresh-plan",
    });

    assert.equal(updated.url, "/plans/fresh-plan");
    assert.equal(updated.date, "2026-06-29");

    const saved = fs.readFileSync(
      path.join(docsDir, "plans", "fresh-plan.md"),
      "utf8"
    );
    assert.match(saved, /^date: 2026-06-29$/m);
    assert.match(saved, /^# Renamed Plan$/m);
    assert.match(saved, /^Updated body\.$/m);

    const indexContent = fs.readFileSync(
      path.join(docsDir, "plans", "index.md"),
      "utf8"
    );
    assert.match(indexContent, /- \[Renamed Plan\]\(\.\/fresh-plan\.md\)/);
    assert.doesNotMatch(indexContent, /- \[Fresh Plan\]\(\.\/fresh-plan\.md\)/);
  });

test("backfillPlanDates adds explicit dates to undated plans", async () => {
  const {backfillPlanDates} = await loadPlanWriterModule();
  const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-plan-writer-"));

  writeMarkdown(
    docsDir,
    "plans/vitepress-live-sidebar-updates-2026-06-29.md",
    [
      "# Live Sidebar",
      "",
      "Body.",
    ].join("\n")
  );
  writeMarkdown(
    docsDir,
    "plans/custom-plan.md",
    [
      "# Custom Plan",
      "",
      "Body.",
    ].join("\n")
  );

  const result = backfillPlanDates(docsDir, {
    fallbackDatesByPath: {
      "plans/custom-plan.md": "2026-06-28",
    },
  });

  assert.deepEqual(
    [...result].sort((left, right) => left.relativePath.localeCompare(right.relativePath)),
    [
      {date: "2026-06-28", relativePath: "plans/custom-plan.md"},
      {date: "2026-06-29", relativePath: "plans/vitepress-live-sidebar-updates-2026-06-29.md"},
    ]
  );

  const inferredFile = fs.readFileSync(
    path.join(docsDir, "plans", "vitepress-live-sidebar-updates-2026-06-29.md"),
    "utf8"
  );
  assert.match(inferredFile, /^date: 2026-06-29$/m);

  const fallbackFile = fs.readFileSync(
    path.join(docsDir, "plans", "custom-plan.md"),
    "utf8"
  );
  assert.match(fallbackFile, /^date: 2026-06-28$/m);
});
