import {spawn} from "node:child_process";

export const DEFAULT_DOCS_BOARD_URL = "http://127.0.0.1:6420/board";

export function buildBrowserOpenSpec(
  url,
  platform = process.platform
) {
  if (platform === "win32") {
    return {
      command: "cmd",
      args: ["/c", "start", "", url],
    };
  }

  if (platform === "darwin") {
    return {
      command: "open",
      args: [url],
    };
  }

  return {
    command: "xdg-open",
    args: [url],
  };
}

export function buildDocsDevServerSpec({
  nodePath = process.execPath,
  npmExecPath = process.env.npm_execpath || "",
  platform = process.platform,
  repoRoot,
}) {
  if (!repoRoot) {
    throw new Error("repoRoot is required");
  }

  if (npmExecPath) {
    return {
      command: nodePath,
      args: [npmExecPath, "run", "docs:dev"],
      cwd: repoRoot,
    };
  }

  return {
    command: platform === "win32" ? "npm.cmd" : "npm",
    args: ["run", "docs:dev"],
    cwd: repoRoot,
  };
}

export async function probeUrl(url, fetchImpl = fetch) {
  try {
    const response = await fetchImpl(url);
    return response.ok;
  } catch {
    return false;
  }
}

export function createUrlOpener({
  platform = process.platform,
  spawnImpl = spawn,
} = {}) {
  return async (url) => {
    const spec = buildBrowserOpenSpec(url, platform);
    const child = spawnImpl(spec.command, spec.args, {
      detached: true,
      stdio: "ignore",
      windowsHide: true,
    });

    child.unref?.();
  };
}

export function createDocsDevServerStarter({
  nodePath = process.execPath,
  npmExecPath = process.env.npm_execpath || "",
  platform = process.platform,
  repoRoot,
  spawnImpl = spawn,
} = {}) {
  return () => {
    const spec = buildDocsDevServerSpec({
      nodePath,
      npmExecPath,
      platform,
      repoRoot,
    });

    return spawnImpl(spec.command, spec.args, {
      cwd: spec.cwd,
      detached: true,
      stdio: "ignore",
      windowsHide: true,
    });
  };
}

function sleep(ms) {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

export async function ensureDocsBoard({
  intervalMs = 500,
  openUrl,
  probe = probeUrl,
  sleep: sleepImpl = sleep,
  startServer,
  timeoutMs = 25_000,
  url = DEFAULT_DOCS_BOARD_URL,
} = {}) {
  if (!openUrl) {
    throw new Error("openUrl is required");
  }

  if (!startServer) {
    throw new Error("startServer is required");
  }

  if (await probe(url)) {
    await openUrl(url);
    return {startedServer: false, url};
  }

  const serverProcess = startServer();
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    await sleepImpl(intervalMs);

    if (await probe(url)) {
      serverProcess.unref?.();
      await openUrl(url);
      return {startedServer: true, url};
    }

    if (serverProcess.exitCode !== null && serverProcess.exitCode !== undefined) {
      throw new Error(
        `The VitePress docs server exited before ${url} became available.`
      );
    }
  }

  throw new Error(`Timed out waiting for the VitePress docs UI at ${url}.`);
}
