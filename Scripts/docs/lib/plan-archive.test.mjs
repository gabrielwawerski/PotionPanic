import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

async function loadPlanArchiveModule() {
  try {
    return await import("../../../Docs/.vitepress/lib/plan-archive.mjs");
  } catch (error) {
    assert.fail(`plan archive module missing: ${error.message}`);
  }
}

function writeMarkdown(root, relativePath, content) {
  const filePath = path.join(root, relativePath);
  fs.mkdirSync(path.dirname(filePath), {recursive: true});
  fs.writeFileSync(filePath, content);
}

test("isArchivablePlanPage only allows non-index plan docs", async () => {
  const {isArchivablePlanPage} = await loadPlanArchiveModule();

  assert.equal(isArchivablePlanPage("plans/feature.md"), true);
  assert.equal(isArchivablePlanPage("plans/index.md"), false);
  assert.equal(isArchivablePlanPage("archive/completed/feature.md"), false);
  assert.equal(isArchivablePlanPage("board.md"), false);
});

test("buildPlanPageUrl converts a plan markdown path into a clean docs url",
  async () => {
    const {buildPlanPageUrl} = await loadPlanArchiveModule();

    assert.equal(
      buildPlanPageUrl("plans/vitepress-archive-flow.md"),
      "/plans/vitepress-archive-flow"
    );
  });

test("archivePlanFile moves a plan into archive completed and updates the index",
  async () => {
    const {archivePlanFile} = await loadPlanArchiveModule();
    const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "pp-plan-archive-"));

    writeMarkdown(
      tempRoot,
      "plans/vitepress-archive-flow.md",
      [
        "---",
        "title: VitePress Archive Flow",
        "---",
        "",
        "# VitePress Archive Flow",
        "",
        "Archive me.",
      ].join("\n")
    );
    writeMarkdown(
      tempRoot,
      "archive/completed/index.md",
      [
        "# Archived Plans",
        "",
        "Completed or superseded implementation plans from `Docs/plans/` live here.",
        "",
        "## Archived Plans",
        "",
        "_No archived plans yet._",
      ].join("\n")
    );

    const archived = archivePlanFile(tempRoot, {
      url: "/plans/vitepress-archive-flow",
    });

    assert.equal(
      fs.existsSync(path.join(tempRoot, "plans", "vitepress-archive-flow.md")),
      false
    );
    assert.equal(
      fs.existsSync(path.join(tempRoot, "archive", "completed",
        "vitepress-archive-flow.md")),
      true
    );
    assert.deepEqual(archived, {
      title: "VitePress Archive Flow",
      url: "/archive/completed/vitepress-archive-flow",
    });

    const indexContent = fs.readFileSync(
      path.join(tempRoot, "archive", "completed", "index.md"),
      "utf8"
    );
    assert.match(indexContent,
      /- \[VitePress Archive Flow\]\(\.\/vitepress-archive-flow\.md\)/);
    assert.doesNotMatch(indexContent, /_No archived plans yet\._/);
  });

test("archivePlanFile rejects plans index pages", async () => {
  const {archivePlanFile} = await loadPlanArchiveModule();
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "pp-plan-archive-"));

  writeMarkdown(tempRoot, "plans/index.md", "# Plans\n");

  assert.throws(() => archivePlanFile(tempRoot, {url: "/plans/"}), /index/i);
});
