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

test("syncActivePlansIndex rebuilds the active plans section from filesystem truth",
  async () => {
    const {syncActivePlansIndex} = await loadPlanWriterModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-plan-writer-"));

    writeMarkdown(
      docsDir,
      "plans/index.md",
      [
        "# Implementation Plans",
        "",
        "Intro text that should stay.",
        "",
        "## Active Plans",
        "",
        "- [Stale Plan](./stale-plan.md)",
        "",
        "## Writing Rules",
        "",
        "- Keep this prose.",
      ].join("\n")
    );
    writeMarkdown(
      docsDir,
      "plans/dated-plan.md",
      [
        "---",
        "date: 2026-06-28",
        "title: Frontmatter Title",
        "---",
        "",
        "# Ignored Heading",
      ].join("\n")
    );
    writeMarkdown(
      docsDir,
      "plans/heading-plan.md",
      [
        "---",
        "date: 2026-06-29",
        "---",
        "",
        "# Heading Title",
      ].join("\n")
    );
    writeMarkdown(
      docsDir,
      "plans/filename-fallback.md",
      [
        "Body only.",
      ].join("\n")
    );

    syncActivePlansIndex(docsDir);

    const indexContent = fs.readFileSync(
      path.join(docsDir, "plans", "index.md"),
      "utf8"
    );

    assert.match(indexContent, /Intro text that should stay\./);
    assert.match(indexContent, /## Writing Rules/);
    assert.doesNotMatch(indexContent, /\[Stale Plan\]/);
    assert.match(indexContent, /- \[Frontmatter Title\]\(\.\/dated-plan\.md\)/);
    assert.match(indexContent, /- \[Heading Title\]\(\.\/heading-plan\.md\)/);
    assert.match(indexContent, /- \[Filename Fallback\]\(\.\/filename-fallback\.md\)/);

    assert.ok(
      indexContent.indexOf("./dated-plan.md")
      < indexContent.indexOf("./heading-plan.md")
    );
    assert.ok(
      indexContent.indexOf("./heading-plan.md")
      < indexContent.indexOf("./filename-fallback.md")
    );
  });

test("syncActivePlansIndex handles the next section heading without a blank separator",
  async () => {
    const {syncActivePlansIndex} = await loadPlanWriterModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-plan-writer-"));

    writeMarkdown(
      docsDir,
      "plans/index.md",
      [
        "# Implementation Plans",
        "",
        "## Active Plans",
        "",
        "- [Stale Plan](./stale-plan.md)",
        "## Writing Rules",
        "",
        "- Keep this prose.",
      ].join("\n")
    );
    writeMarkdown(
      docsDir,
      "plans/real-plan.md",
      [
        "# Real Plan",
      ].join("\n")
    );

    syncActivePlansIndex(docsDir);

    const indexContent = fs.readFileSync(
      path.join(docsDir, "plans", "index.md"),
      "utf8"
    );

    assert.doesNotMatch(indexContent, /\[Stale Plan\]/);
    assert.match(indexContent, /- \[Real Plan\]\(\.\/real-plan\.md\)/);
    assert.match(indexContent, /\n## Writing Rules/);
  });

test("syncActivePlansIndex fully replaces a CRLF active plans section without stale bullets",
  async () => {
    const {syncActivePlansIndex} = await loadPlanWriterModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-plan-writer-"));

    writeMarkdown(
      docsDir,
      "plans/index.md",
      [
        "# Implementation Plans",
        "",
        "## Active Plans",
        "",
        "- [Old One](old-one.md)",
        "- [Old Two](old-two.md)",
        "## Writing Rules",
        "",
        "- Keep this prose.",
        "",
      ].join("\r\n")
    );
    writeMarkdown(
      docsDir,
      "plans/real-plan.md",
      [
        "# Real Plan",
      ].join("\n")
    );

    syncActivePlansIndex(docsDir);

    const indexContent = fs.readFileSync(
      path.join(docsDir, "plans", "index.md"),
      "utf8"
    );

    assert.doesNotMatch(indexContent, /\[Old One\]/);
    assert.doesNotMatch(indexContent, /\[Old Two\]/);
    assert.doesNotMatch(indexContent, /- \[\]\(\.\/\)/);
    assert.match(indexContent, /- \[Real Plan\]\(\.\/real-plan\.md\)/);
    assert.match(indexContent, /## Writing Rules/);
  });
