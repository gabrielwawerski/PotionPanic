import path from "node:path";

import {
  DEFAULT_EXCLUDED_DIRS,
  buildSidebarThemeConfig,
  isSidebarContentPath,
} from "./sidebar.mjs";
import {syncActivePlansIndex} from "./plan-writer.mjs";

function normalizePath(value) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/");
}

export function sidebarHmrPlugin({
  excludedDirs = DEFAULT_EXCLUDED_DIRS,
  sections = [],
} = {}) {
  let srcDir = "";

  function buildPayload() {
    return {
      type: "custom",
      event: "potion-panic:sidebar-update",
      data: {
        sidebar: buildSidebarThemeConfig(srcDir, {excludedDirs, sections}),
      },
    };
  }

  function isRelevantFile(file) {
    const relativePath = normalizePath(path.relative(srcDir, file));
    return isSidebarContentPath(relativePath, {excludedDirs, sections});
  }

  function isSyncablePlanFile(file) {
    const relativePath = normalizePath(path.relative(srcDir, file));
    return relativePath.startsWith("plans/")
      && relativePath.endsWith(".md")
      && relativePath !== "plans/index.md";
  }

  async function emitUpdate(server, file) {
    if (!srcDir || !isRelevantFile(file)) {
      return;
    }

    if (isSyncablePlanFile(file)) {
      syncActivePlansIndex(srcDir);
    }

    server.ws.send(buildPayload());
  }

  return {
    name: "potion-panic-sidebar-hmr",
    configResolved(config) {
      srcDir = config.root;
    },
    configureServer(server) {
      server.watcher
      .on("add", (file) => emitUpdate(server, file))
      .on("unlink", (file) => emitUpdate(server, file));
    },
    async handleHotUpdate({file, server}) {
      await emitUpdate(server, file);
    },
  };
}
