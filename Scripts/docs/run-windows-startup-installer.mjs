#!/usr/bin/env node

import {execFileSync} from "node:child_process";
import path from "node:path";
import {fileURLToPath} from "node:url";

if (process.platform !== "win32") {
  console.error("The docs Windows startup installer only supports Windows.");
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
const installerScriptPath = path.join(
  scriptDirectory,
  "install-windows-startup.ps1"
);

try {
  execFileSync(powershellPath, [
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    installerScriptPath,
    ...process.argv.slice(2),
  ], {
    stdio: "inherit",
  });
} catch (error) {
  process.exit(error.status ?? 1);
}
