import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const plansIndexPath = path.resolve("Docs/plans/index.md");
const plansIndexSource = fs.readFileSync(plansIndexPath, "utf8");

function readMarkdownLinks(source) {
  return Array.from(source.matchAll(/\[[^\]]+\]\(([^)]+\.md)\)/g), (match) => (
    match[1]
  ));
}

test("Docs/plans/index.md only links to markdown files that exist", () => {
  const links = readMarkdownLinks(plansIndexSource)
    .filter((link) => !link.startsWith("../"));

  assert.ok(links.length > 0, "expected at least one local markdown link");

  for (const link of links) {
    const resolvedPath = path.resolve("Docs/plans", link);
    assert.equal(
      fs.existsSync(resolvedPath),
      true,
      `expected Docs/plans/index.md link to resolve: ${link}`
    );
  }
});
