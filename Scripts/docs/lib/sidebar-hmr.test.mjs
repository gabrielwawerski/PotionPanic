import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

async function loadSidebarModule() {
  try {
    return await import("../../../Docs/.vitepress/lib/sidebar.mjs");
  } catch (error) {
    assert.fail(`sidebar module missing: ${error.message}`);
  }
}

async function loadSidebarHmrPluginModule() {
  try {
    return await import("../../../Docs/.vitepress/lib/sidebar-hmr-plugin.mjs");
  } catch (error) {
    assert.fail(`sidebar HMR plugin missing: ${error.message}`);
  }
}

function writeMarkdown(root, relativePath, content) {
  const filePath = path.join(root, relativePath);
  fs.mkdirSync(path.dirname(filePath), {recursive: true});
  fs.writeFileSync(filePath, content);
}

test("buildSidebarThemeConfig returns the root sidebar mapping", async () => {
  const {buildSidebarThemeConfig, SIDEBAR_SECTIONS} = await loadSidebarModule();
  const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-hmr-"));

  writeMarkdown(docsDir, "index.md", "# Docs Index\n");
  writeMarkdown(docsDir, "board.md", "---\nboard: true\n---\n");
  writeMarkdown(docsDir, "plans/index.md", "# Plans\n");
  writeMarkdown(docsDir, "plans/live-sidebar.md", "# Live Sidebar\n");

  const sidebar = buildSidebarThemeConfig(docsDir);

  assert.ok(Array.isArray(SIDEBAR_SECTIONS));
  assert.deepEqual(sidebar, {
    "/": [
      {
        text: "Docs",
        items: [
          {text: "Overview", link: "/"},
          {text: "Board", link: "/board"},
        ],
      },
      {
        text: "Onboarding",
        items: [
          {text: "Getting Started", link: "/onboarding/getting-started"},
        ],
      },
      {
        text: "Collaboration",
        items: [
          {text: "Team Workflow", link: "/collaboration/team-workflow"},
        ],
      },
      {
        text: "Project",
        items: [
          {text: "Game Design", link: "/project/game-design"},
          {text: "MVP Scope", link: "/project/mvp-scope"},
          {text: "Technical Architecture", link: "/project/technical-architecture"},
          {text: "Game Design and Psychology", link: "/project/game-design-and-psychology"},
        ],
      },
      {
        text: "Plans",
        items: [
          {text: "Implementation Plans", link: "/plans/"},
          {text: "Live Sidebar", link: "/plans/live-sidebar"},
        ],
      },
      {
        text: "Guides",
        items: [
          {text: "Unity Guides", link: "/guides/unity/"},
          {text: "Runtime Architecture", link: "/guides/unity/runtime-architecture"},
          {
            text: "Coding And Implementation",
            link: "/guides/unity/coding-and-implementation",
          },
          {text: "Editor Safety", link: "/guides/unity/editor-safety"},
          {
            text: "Presentation Workflows",
            link: "/guides/unity/presentation-workflows",
          },
        ],
      },
      {
        text: "Planning History",
        items: [
          {text: "Milestones", link: "/milestones/"},
          {text: "Archive Board", link: "/archive/board"},
          {text: "Archive", link: "/archive/"},
          {text: "Archived Plans", link: "/archive/completed/"},
        ],
      },
    ],
  });
});

test("sidebar HMR plugin emits updates only for relevant docs markdown paths",
  async () => {
    const {sidebarHmrPlugin} = await loadSidebarHmrPluginModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-hmr-"));

    writeMarkdown(docsDir, "index.md", "# Docs Index\n");
    writeMarkdown(docsDir, "board.md", "---\nboard: true\n---\n");
    writeMarkdown(docsDir, "plans/index.md", "# Plans\n");
    writeMarkdown(docsDir, "plans/live-sidebar.md",
      "---\ndate: 2026-06-29\n---\n# Live Sidebar\n");
    writeMarkdown(docsDir, "tickets/PP-1.md", "# Ticket\n");

    const sent = [];
    const addHandlers = [];
    const unlinkHandlers = [];
    const watcher = {
      on(event, handler) {
        if (event === "add") {
          addHandlers.push(handler);
        }
        if (event === "unlink") {
          unlinkHandlers.push(handler);
        }
        return watcher;
      },
    };

    const plugin = sidebarHmrPlugin();
    plugin.configResolved({root: docsDir});
    const server = {
      watcher,
      ws: {
        send(payload) {
          sent.push(payload);
        },
      },
    };
    plugin.configureServer(server);

    await plugin.handleHotUpdate({
      file: path.join(docsDir, "plans", "live-sidebar.md"),
      server,
    });
    writeMarkdown(docsDir, "plans/new-plan.md",
      "---\ndate: 2026-06-27\n---\n# New Plan\n");
    await addHandlers[0](path.join(docsDir, "plans", "new-plan.md"));
    await unlinkHandlers[0](path.join(docsDir, "tickets", "PP-1.md"));

    assert.equal(sent.length, 2);
    assert.deepEqual(sent.map((payload) => payload.event), [
      "potion-panic:sidebar-update",
      "potion-panic:sidebar-update",
    ]);
    assert.equal(sent[0].type, "custom");
    assert.equal(sent[0].data.sidebar["/"][4].items.at(-1).text, "Live Sidebar");
    assert.deepEqual(sent[1].data.sidebar["/"][4].items, [
      {text: "Implementation Plans", link: "/plans/"},
      {text: "New Plan", link: "/plans/new-plan"},
      {text: "Live Sidebar", link: "/plans/live-sidebar"},
    ]);
  });
