import assert from "node:assert/strict";
import test from "node:test";

import {
  buildBrowserOpenSpec,
  buildDocsDevServerSpec,
  ensureDocsBoard,
} from "./docs-ui-launcher.mjs";

test("buildBrowserOpenSpec returns the Windows browser command", () => {
  assert.deepEqual(buildBrowserOpenSpec("http://127.0.0.1:6420/board", "win32"),
    {
      command: "cmd",
      args: ["/c", "start", "", "http://127.0.0.1:6420/board"],
    });
});

test("buildBrowserOpenSpec returns the macOS browser command", () => {
  assert.deepEqual(buildBrowserOpenSpec("http://127.0.0.1:6420/board", "darwin"),
    {
      command: "open",
      args: ["http://127.0.0.1:6420/board"],
    });
});

test("buildDocsDevServerSpec prefers npm_execpath when available", () => {
  assert.deepEqual(buildDocsDevServerSpec({
    nodePath: "/node",
    npmExecPath: "/npm-cli.js",
    platform: "linux",
    repoRoot: "/repo",
  }), {
    command: "/node",
    args: ["/npm-cli.js", "run", "docs:dev"],
    cwd: "/repo",
  });
});

test("buildDocsDevServerSpec falls back to npm.cmd on Windows", () => {
  assert.deepEqual(buildDocsDevServerSpec({
    nodePath: "C:/Program Files/nodejs/node.exe",
    npmExecPath: "",
    platform: "win32",
    repoRoot: "C:/Dev/PotionPanic",
  }), {
    command: "npm.cmd",
    args: ["run", "docs:dev"],
    cwd: "C:/Dev/PotionPanic",
  });
});

test("buildDocsDevServerSpec accepts an npm script override", () => {
  assert.deepEqual(buildDocsDevServerSpec({
    nodePath: "/node",
    npmExecPath: "/npm-cli.js",
    platform: "linux",
    repoRoot: "/repo",
    scriptName: "docs:dev:local",
  }), {
    command: "/node",
    args: ["/npm-cli.js", "run", "docs:dev:local"],
    cwd: "/repo",
  });
});

test("ensureDocsBoard reuses an already-running board", async () => {
  const opened = [];
  let startCount = 0;

  const result = await ensureDocsBoard({
    openUrl: async (url) => {
      opened.push(url);
    },
    probe: async () => true,
    startServer: () => {
      startCount += 1;
      return {exitCode: null};
    },
  });

  assert.equal(startCount, 0);
  assert.deepEqual(opened, ["http://127.0.0.1:6420/board"]);
  assert.equal(result.startedServer, false);
});

test("ensureDocsBoard starts the docs server when the board is offline",
  async () => {
    const opened = [];
    let startCount = 0;
    let probeCount = 0;

    const result = await ensureDocsBoard({
      intervalMs: 1,
      openUrl: async (url) => {
        opened.push(url);
      },
      probe: async () => {
        probeCount += 1;
        return probeCount >= 2;
      },
      sleep: async () => {},
      startServer: () => {
        startCount += 1;
        return {exitCode: null};
      },
    });

    assert.equal(startCount, 1);
    assert.deepEqual(opened, ["http://127.0.0.1:6420/board"]);
    assert.equal(result.startedServer, true);
  });

test("ensureDocsBoard fails when the spawned docs server exits early",
  async () => {
    await assert.rejects(() => ensureDocsBoard({
      intervalMs: 1,
      openUrl: async () => {},
      probe: async () => false,
      sleep: async () => {},
      startServer: () => ({exitCode: 1}),
    }), /exited before http:\/\/127\.0\.0\.1:6420\/board became available/i);
  });

test("ensureDocsBoard fails when the board never becomes reachable",
  async () => {
    await assert.rejects(() => ensureDocsBoard({
      intervalMs: 1,
      openUrl: async () => {},
      probe: async () => false,
      sleep: async () => {},
      startServer: () => ({exitCode: null}),
      timeoutMs: 2,
    }), /timed out waiting for the VitePress docs UI/i);
  });
