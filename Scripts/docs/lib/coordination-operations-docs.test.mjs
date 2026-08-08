import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const serverReadmePath = path.resolve("Tools/CoordinationServer/README.md");
const rootReadmePath = path.resolve("README.md");
const onboardingPath = path.resolve("Docs/onboarding/getting-started.md");
const workflowPath = path.resolve("Docs/collaboration/team-workflow.md");
const localSecretsTemplatePath = path.resolve(
  "Tools/CoordinationServer/.dev.vars.example"
);

test("coordination server README documents secure operator procedures", () => {
  const source = fs.readFileSync(serverReadmePath, "utf8");

  for (const requiredText of [
    "TOKEN_HMAC_KEY",
    "ADMIN_TOKEN",
    ".dev.vars.example",
    "npx wrangler tail",
    "node scripts/issue-token.mjs",
  ]) {
    assert.ok(
      source.includes(requiredText),
      `server README should contain ${requiredText}`
    );
  }

  assert.match(
    source,
    /npx wrangler deploy --strict --secrets-file \$secretFile/u,
    "server README should document an actual manual deployment, not only a dry run"
  );
});

test("root README keeps coordination deployment manual and GitHub verification-only", () => {
  const source = fs.readFileSync(rootReadmePath, "utf8");

  assert.match(source, /coordination Worker[^\n]*deploy[^\n]*manual/i);
  assert.match(source, /GitHub[^\n]*verification-only/i);
});

test("evergreen collaboration docs describe Disabled and manual fallback", () => {
  for (const documentationPath of [onboardingPath, workflowPath]) {
    const source = fs.readFileSync(documentationPath, "utf8");

    assert.match(source, /local [`']?Disabled[`']? switch/i);
    assert.match(source, /manual collaboration/i);
  }
});

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
    if (filePath.endsWith("package-lock.json")) {
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
      if (child.name === "node_modules") {
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
