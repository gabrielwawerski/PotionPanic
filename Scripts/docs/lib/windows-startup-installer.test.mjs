import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import {execFile} from "node:child_process";
import test from "node:test";
import {promisify} from "node:util";

const execFileAsync = promisify(execFile);

const repoRoot = path.resolve(process.cwd());
const powershellPath = path.join(
  process.env.SystemRoot || "C:\\Windows",
  "System32",
  "WindowsPowerShell",
  "v1.0",
  "powershell.exe"
);
const installerScriptPath = path.join(
  repoRoot,
  "Scripts",
  "docs",
  "install-windows-startup.ps1"
);

const canonicalShortcutName = "PotionPanic - Start Docs Server.lnk";
const legacyShortcutNames = [
  "PotionPanic - Start Backlog Server.lnk",
  "PotionPanic - Open Backlog Board.lnk",
];

async function invokePowerShell(command) {
  return execFileAsync(powershellPath, [
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-Command",
    command,
  ], {
    cwd: repoRoot,
  });
}

async function readShortcut(shortcutPath) {
  const escapedPath = shortcutPath.replace(/'/g, "''");
  const {stdout} = await invokePowerShell(
    [
      "$shell = New-Object -ComObject WScript.Shell",
      `$shortcut = $shell.CreateShortcut('${escapedPath}')`,
      "$data = [ordered]@{ " +
        "TargetPath = $shortcut.TargetPath; " +
        "Arguments = $shortcut.Arguments; " +
        "WorkingDirectory = $shortcut.WorkingDirectory " +
      "}",
      "$data | ConvertTo-Json -Compress",
    ].join("; ")
  );

  return JSON.parse(stdout.trim());
}

function decodeEncodedCommand(argumentsText) {
  const match = argumentsText.match(/-EncodedCommand\s+([A-Za-z0-9+/=]+)/i);
  assert.notEqual(match, null, "shortcut should include -EncodedCommand");
  return Buffer.from(match[1], "base64").toString("utf16le");
}

test("install-windows-startup script creates a local-only docs shortcut",
  {skip: process.platform !== "win32"},
  async () => {
    const tempRoot = fs.mkdtempSync(
      path.join(os.tmpdir(), "potion-panic-startup-test-")
    );
    const startupDir = path.join(tempRoot, "Startup");
    fs.mkdirSync(startupDir, {recursive: true});

    await execFileAsync(powershellPath, [
      "-NoProfile",
      "-ExecutionPolicy",
      "Bypass",
      "-File",
      installerScriptPath,
      "-StartupFolderPath",
      startupDir,
    ], {
      cwd: repoRoot,
    });

    const shortcutPath = path.join(startupDir, canonicalShortcutName);
    assert.equal(fs.existsSync(shortcutPath), true);
  });

test("install-windows-startup script is idempotent and uninstall removes shortcuts",
  {skip: process.platform !== "win32"},
  async () => {
    const tempRoot = fs.mkdtempSync(
      path.join(os.tmpdir(), "potion-panic-startup-test-")
    );
    const startupDir = path.join(tempRoot, "Startup");
    fs.mkdirSync(startupDir, {recursive: true});

    for (const shortcutName of legacyShortcutNames) {
      fs.writeFileSync(path.join(startupDir, shortcutName), "legacy");
    }

    await execFileAsync(powershellPath, [
      "-NoProfile",
      "-ExecutionPolicy",
      "Bypass",
      "-File",
      installerScriptPath,
      "-StartupFolderPath",
      startupDir,
    ], {
      cwd: repoRoot,
    });

    const shortcutPath = path.join(startupDir, canonicalShortcutName);
    const shortcut = await readShortcut(shortcutPath);

    assert.equal(
      shortcut.WorkingDirectory,
      repoRoot,
      "shortcut should run from the repo root"
    );
    assert.match(
      shortcut.TargetPath,
      /powershell\.exe$/i,
      "shortcut should target powershell.exe"
    );
    const decodedCommand = decodeEncodedCommand(shortcut.Arguments);

    assert.match(decodedCommand, /docs:dev:local/);
    assert.doesNotMatch(decodedCommand, /docs:ui/i);
    assert.doesNotMatch(decodedCommand, /open-board\.mjs/i);
    assert.doesNotMatch(decodedCommand, /cmd\s*\/c\s+start/i);
    assert.doesNotMatch(decodedCommand, /Start-Process/i);

    await execFileAsync(powershellPath, [
      "-NoProfile",
      "-ExecutionPolicy",
      "Bypass",
      "-File",
      installerScriptPath,
      "-StartupFolderPath",
      startupDir,
    ], {
      cwd: repoRoot,
    });

    const startupEntries = fs.readdirSync(startupDir);
    assert.deepEqual(startupEntries, [canonicalShortcutName]);

    await execFileAsync(powershellPath, [
      "-NoProfile",
      "-ExecutionPolicy",
      "Bypass",
      "-File",
      installerScriptPath,
      "-StartupFolderPath",
      startupDir,
      "-Uninstall",
    ], {
      cwd: repoRoot,
    });

    assert.deepEqual(fs.readdirSync(startupDir), []);
  });
