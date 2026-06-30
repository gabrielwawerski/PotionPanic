#!/usr/bin/env node

import {execFileSync} from "node:child_process";
import path from "node:path";
import {fileURLToPath} from "node:url";

function parseArgs(argv) {
  let port = 6420;

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];

    if (arg === "--port") {
      const value = argv[index + 1];
      const parsed = Number.parseInt(value || "", 10);

      if (!Number.isInteger(parsed) || parsed <= 0) {
        throw new Error("Expected a positive integer after --port.");
      }

      port = parsed;
      index += 1;
      continue;
    }

    throw new Error(`Unsupported argument: ${arg}`);
  }

  return {port};
}

if (process.platform !== "win32") {
  console.error("The docs stop command only supports Windows.");
  process.exit(1);
}

const filePath = fileURLToPath(import.meta.url);
const scriptDirectory = path.dirname(filePath);
const powershellPath = path.join(
  process.env.SystemRoot || "C:\\Windows",
  "System32",
  "WindowsPowerShell",
  "v1.0",
  "powershell.exe"
);
const stopScriptPath = path.join(
  scriptDirectory,
  "stop-docs-server.ps1"
);

let port;

try {
  ({port} = parseArgs(process.argv.slice(2)));
} catch (error) {
  console.error(String(error.message || error));
  process.exit(1);
}

try {
  const stdout = execFileSync(powershellPath, [
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    stopScriptPath,
    "-Port",
    String(port),
  ], {
    encoding: "utf8",
  });

  if (stdout) {
    process.stdout.write(stdout);
  }
} catch (error) {
  if (error.stdout) {
    process.stdout.write(error.stdout);
  }

  if (error.stderr) {
    process.stderr.write(error.stderr);
  }

  process.exit(error.status ?? 1);
}
