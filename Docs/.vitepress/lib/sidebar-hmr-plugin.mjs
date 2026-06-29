import path from "node:path";

import {
  buildSidebarThemeConfig,
  isSidebarContentPath,
} from "./sidebar.mjs";

function normalizePath(value) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/");
}

export function sidebarHmrPlugin() {
  let srcDir = "";

  function buildPayload() {
    return {
      type: "custom",
      event: "potion-panic:sidebar-update",
      data: {
        sidebar: buildSidebarThemeConfig(srcDir),
      },
    };
  }

  function isRelevantFile(file) {
    const relativePath = normalizePath(path.relative(srcDir, file));
    return isSidebarContentPath(relativePath);
  }

  async function emitUpdate(server, file) {
    if (!srcDir || !isRelevantFile(file)) {
      return;
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
