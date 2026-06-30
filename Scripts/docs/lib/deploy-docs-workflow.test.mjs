import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const workflowPath = path.resolve(".github/workflows/deploy-docs.yml");
const readmePath = path.resolve("README.md");

test("deploy docs workflow supports optional first-run Pages enablement", () => {
  const source = fs.readFileSync(workflowPath, "utf8");

  assert.match(
    source,
    /if:\s*\$\{\{\s*secrets\.PAGES_ENABLEMENT_TOKEN\s*!=\s*''\s*\}\}/,
    "workflow should branch when an enablement token is available"
  );
  assert.match(
    source,
    /enablement:\s*true/,
    "workflow should request Pages enablement when the dedicated token exists"
  );
  assert.match(
    source,
    /token:\s*\$\{\{\s*secrets\.PAGES_ENABLEMENT_TOKEN\s*\}\}/,
    "workflow should use the dedicated Pages enablement token"
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
});
