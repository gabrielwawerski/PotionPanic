import assert from "node:assert/strict";
import {spawn, execFile} from "node:child_process";
import {once} from "node:events";
import net from "node:net";
import path from "node:path";
import test from "node:test";
import {promisify} from "node:util";

const execFileAsync = promisify(execFile);

const repoRoot = path.resolve(process.cwd());
const stopScriptPath = path.join(
  repoRoot,
  "Scripts",
  "docs",
  "run-windows-docs-stop.mjs"
);

function sleep(ms) {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

async function findOpenPort() {
  const server = net.createServer();

  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });

  const {port} = server.address();
  await new Promise((resolve, reject) => {
    server.close((error) => {
      if (error) {
        reject(error);
        return;
      }
      resolve();
    });
  });

  return port;
}

async function isPortOpen(port) {
  return new Promise((resolve) => {
    const socket = net.createConnection({
      host: "127.0.0.1",
      port,
    });

    socket.once("connect", () => {
      socket.destroy();
      resolve(true);
    });

    socket.once("error", () => {
      resolve(false);
    });
  });
}

async function waitForPortState(port, expectedOpen, timeoutMs = 10_000) {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    if (await isPortOpen(port)) {
      if (expectedOpen) {
        return;
      }
    } else if (!expectedOpen) {
      return;
    }

    await sleep(100);
  }

  throw new Error(
    `Timed out waiting for port ${port} to become ${
      expectedOpen ? "open" : "closed"
    }.`
  );
}

function startTestServer(port) {
  return spawn(process.execPath, [
    "-e",
    [
      "const http = require('node:http');",
      "const port = Number(process.argv[1]);",
      "const server = http.createServer((request, response) => response.end('ok'));",
      "server.listen(port, '127.0.0.1');",
      "setInterval(() => {}, 1000);",
    ].join(" "),
    String(port),
  ], {
    cwd: repoRoot,
    stdio: "ignore",
    windowsHide: true,
  });
}

test("windows docs stop script exits cleanly when no listener exists",
  {skip: process.platform !== "win32"},
  async () => {
    const port = await findOpenPort();
    const {stdout} = await execFileAsync(process.execPath, [
      stopScriptPath,
      "--port",
      String(port),
    ], {
      cwd: repoRoot,
    });

    assert.match(stdout, new RegExp(`No process is listening on port ${port}`));
  });

test("windows docs stop script stops a background listener on the target port",
  {skip: process.platform !== "win32"},
  async () => {
    const port = await findOpenPort();
    const serverProcess = startTestServer(port);

    try {
      await waitForPortState(port, true);

      const {stdout} = await execFileAsync(process.execPath, [
        stopScriptPath,
        "--port",
        String(port),
      ], {
        cwd: repoRoot,
      });

      assert.match(
        stdout,
        new RegExp(`Stopped process\\(es\\) on port ${port}:`)
      );

      await waitForPortState(port, false);

      if (serverProcess.exitCode === null) {
        await Promise.race([
          once(serverProcess, "exit"),
          sleep(10_000).then(() => {
            throw new Error("Timed out waiting for the test server process to exit.");
          }),
        ]);
      }
    } finally {
      if (serverProcess.exitCode === null) {
        serverProcess.kill();
      }
    }
  });
