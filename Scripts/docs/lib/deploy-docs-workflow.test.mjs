import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const workflowPath = path.resolve(".github/workflows/deploy-docs.yml");
const packagePath = path.resolve("package.json");

test("deploy docs workflow fetches full history for docs timestamps", () => {
  const source = fs.readFileSync(workflowPath, "utf8");

  assert.match(
    source,
    /uses:\s*actions\/checkout@v5\s+with:\s+fetch-depth:\s*0/,
    "workflow should fetch full history for VitePress lastUpdated metadata"
  );
});

test("deploy docs workflow installs the checked-out private Docboard dependency", () => {
  const source = fs.readFileSync(workflowPath, "utf8");
  const packageJson = JSON.parse(fs.readFileSync(packagePath, "utf8"));

  assert.match(source, /git clone --depth 1[\s\S]*\.\.\/Docboard/);
  assert.strictEqual(packageJson.devDependencies["@gabrielwawerski/docboard"], "file:../Docboard");
});

test("deploy docs workflow installs Docboard's build dependencies", () => {
  const source = fs.readFileSync(workflowPath, "utf8");

  assert.match(source, /working-directory:\s*\.\.\/Docboard\s+run:\s*npm ci/);
});
