import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const localSecretsTemplatePath = path.resolve(
  "Tools/CoordinationServer/.dev.vars.example"
);

test("local secret template contains only empty required assignments", () => {
  const source = fs.readFileSync(localSecretsTemplatePath, "utf8");
  const assignments = source
    .split(/\r?\n/u)
    .filter((line) => /^[A-Z][A-Z0-9_]*=/u.test(line));

  assert.deepStrictEqual(assignments, ["TOKEN_HMAC_KEY=", "ADMIN_TOKEN="]);
});

test("coordination documentation and tools contain no real-looking bearer token", () => {
  const bearerToken = /Bearer\s+[A-Za-z0-9_-]{20,}/u;
  const roots = [
    path.resolve("README.md"),
    path.resolve("Docs"),
    path.resolve("Tools/CoordinationServer"),
  ];

  for (const filePath of collectFiles(roots)) {
    if (filePath.endsWith("package-lock.json") || isSyntheticFixture(filePath)) {
      continue;
    }

    const source = fs.readFileSync(filePath, "utf8");
    assert.doesNotMatch(source, bearerToken, `${filePath} contains a bearer token`);
  }
});

function collectFiles(entries) {
  const files = [];

  for (const entry of entries) {
    const stats = fs.statSync(entry);
    if (stats.isFile()) {
      files.push(entry);
      continue;
    }

    for (const child of fs.readdirSync(entry, { withFileTypes: true })) {
      if (child.name === "node_modules" || child.name === ".wrangler") {
        continue;
      }

      const childPath = path.join(entry, child.name);
      if (child.isDirectory()) {
        files.push(...collectFiles([childPath]));
      } else {
        files.push(childPath);
      }
    }
  }

  return files;
}

function isSyntheticFixture(filePath) {
  const segments = path.relative(process.cwd(), filePath).split(path.sep);
  return segments.includes("test") || segments.includes("tests");
}
