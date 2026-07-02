import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const workflowPath = path.resolve(".github/workflows/deploy-docs.yml");
const readmePath = path.resolve("README.md");

test("deploy docs workflow fetches full history for docs timestamps", () => {
  const source = fs.readFileSync(workflowPath, "utf8");

  assert.match(
    source,
    /uses:\s*actions\/checkout@v5\s+with:\s+fetch-depth:\s*0/,
    "workflow should fetch full history for VitePress lastUpdated metadata"
  );
});

test("README documents the manual Pages settings fallback", () => {
  const source = fs.readFileSync(readmePath, "utf8");

  assert.match(
    source,
    /Settings > Pages/,
    "README should point maintainers to the repository Pages settings"
  );
  assert.match(
    source,
    /GitHub Actions/,
    "README should state that the Pages source must be GitHub Actions"
  );
  assert.match(
    source,
    /do that once manually/,
    "README should describe the manual first-run Pages enablement fallback"
  );
});
