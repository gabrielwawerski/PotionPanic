#!/usr/bin/env node

import path from "node:path";
import {fileURLToPath} from "node:url";

import {
  createDocsDevServerStarter,
  createUrlOpener,
  ensureDocsBoard,
} from "./lib/docs-ui-launcher.mjs";

const filePath = fileURLToPath(import.meta.url);
const scriptDirectory = path.dirname(filePath);
const repositoryRoot = path.resolve(scriptDirectory, "..", "..");

try {
  await ensureDocsBoard({
    openUrl: createUrlOpener(),
    startServer: createDocsDevServerStarter({
      repoRoot: repositoryRoot,
    }),
  });
} catch (cause) {
  console.error(String(cause));
  process.exitCode = 1;
}
