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

const workflowSidebarOptions = {
  sections: [
    {
      text: "Start Here",
      items: [
        {text: "Docs Home", link: "/"},
        {text: "Getting Started", link: "/onboarding/getting-started"},
        {text: "Team Workflow", link: "/collaboration/team-workflow"},
      ],
    },
    {
      text: "Active Work",
      includeDirs: ["plans"],
      items: [
        {text: "Board", link: "/board"},
        {text: "Implementation Plans", link: "/plans/"},
        {text: "Milestones", link: "/milestones/"},
      ],
    },
    {
      text: "Project Truth",
      items: [
        {text: "Game Design", link: "/project/game-design"},
        {text: "MVP Scope", link: "/project/mvp-scope"},
        {
          text: "Technical Architecture",
          link: "/project/technical-architecture",
        },
        {
          text: "Game Design And Psychology",
          link: "/project/game-design-and-psychology",
        },
      ],
    },
    {
      text: "Unity Guides",
      items: [
        {text: "Guides", link: "/guides/unity/"},
        {
          text: "Runtime Architecture",
          link: "/guides/unity/runtime-architecture",
        },
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
      text: "Archive",
      items: [
        {text: "Archive", link: "/archive/"},
        {text: "Archive Board", link: "/archive/board"},
        {text: "Archived Plans", link: "/archive/completed/"},
      ],
    },
  ],
};

test("buildSidebarThemeConfig returns the root sidebar mapping", async () => {
  const {buildSidebarThemeConfig} = await loadSidebarModule();
  const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-hmr-"));

  writeMarkdown(docsDir, "index.md", "# Docs Index\n");
  writeMarkdown(docsDir, "board.md", "---\nboard: true\n---\n");
  writeMarkdown(docsDir, "plans/index.md", "# Plans\n");
  writeMarkdown(docsDir, "plans/live-sidebar.md", "# Live Sidebar\n");

  const sidebar = buildSidebarThemeConfig(docsDir, workflowSidebarOptions);

  assert.deepEqual(sidebar, {
    "/": [
      {
        text: "Start Here",
        items: [
          {text: "Docs Home", link: "/"},
          {text: "Getting Started", link: "/onboarding/getting-started"},
          {text: "Team Workflow", link: "/collaboration/team-workflow"},
        ],
      },
      {
        text: "Active Work",
        items: [
          {text: "Board", link: "/board"},
          {text: "Implementation Plans", link: "/plans/"},
          {text: "Milestones", link: "/milestones/"},
          {text: "Live Sidebar", link: "/plans/live-sidebar"},
        ],
      },
      {
        text: "Project Truth",
        items: [
          {text: "Game Design", link: "/project/game-design"},
          {text: "MVP Scope", link: "/project/mvp-scope"},
          {
            text: "Technical Architecture",
            link: "/project/technical-architecture",
          },
          {
            text: "Game Design And Psychology",
            link: "/project/game-design-and-psychology",
          },
        ],
      },
      {
        text: "Unity Guides",
        items: [
          {text: "Guides", link: "/guides/unity/"},
          {
            text: "Runtime Architecture",
            link: "/guides/unity/runtime-architecture",
          },
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
        text: "Archive",
        items: [
          {text: "Archive", link: "/archive/"},
          {text: "Archive Board", link: "/archive/board"},
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

    const plugin = sidebarHmrPlugin(workflowSidebarOptions);
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
    assert.equal(sent[0].data.sidebar["/"][1].items.at(-1).text, "Live Sidebar");
    assert.deepEqual(sent[1].data.sidebar["/"][1].items, [
      {text: "Board", link: "/board"},
      {text: "Implementation Plans", link: "/plans/"},
      {text: "Milestones", link: "/milestones/"},
      {text: "New Plan", link: "/plans/new-plan"},
      {text: "Live Sidebar", link: "/plans/live-sidebar"},
    ]);
  });

test("sidebar HMR plugin syncs the active plans index for manual plan file changes",
  async () => {
    const {sidebarHmrPlugin} = await loadSidebarHmrPluginModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-hmr-"));

    writeMarkdown(
      docsDir,
      "plans/index.md",
      [
        "# Implementation Plans",
        "",
        "## Active Plans",
        "",
        "_No active plans yet._",
      ].join("\n")
    );
    writeMarkdown(docsDir, "plans/live-sidebar.md",
      "---\ndate: 2026-06-29\n---\n# Live Sidebar\n");

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

    const plugin = sidebarHmrPlugin(workflowSidebarOptions);
    plugin.configResolved({root: docsDir});
    const server = {
      watcher,
      ws: {
        send() {},
      },
    };
    plugin.configureServer(server);

    writeMarkdown(docsDir, "plans/new-plan.md",
      "---\ndate: 2026-06-27\n---\n# New Plan\n");
    await addHandlers[0](path.join(docsDir, "plans", "new-plan.md"));

    let indexContent = fs.readFileSync(
      path.join(docsDir, "plans", "index.md"),
      "utf8"
    );
    assert.match(indexContent, /- \[New Plan\]\(\.\/new-plan\.md\)/);
    assert.match(indexContent, /- \[Live Sidebar\]\(\.\/live-sidebar\.md\)/);

    writeMarkdown(docsDir, "plans/new-plan.md",
      "---\ndate: 2026-06-27\n---\n# Renamed Plan\n");
    await plugin.handleHotUpdate({
      file: path.join(docsDir, "plans", "new-plan.md"),
      server,
    });

    indexContent = fs.readFileSync(
      path.join(docsDir, "plans", "index.md"),
      "utf8"
    );
    assert.match(indexContent, /- \[Renamed Plan\]\(\.\/new-plan\.md\)/);
    assert.doesNotMatch(indexContent, /- \[New Plan\]\(\.\/new-plan\.md\)/);

    fs.unlinkSync(path.join(docsDir, "plans", "new-plan.md"));
    await unlinkHandlers[0](path.join(docsDir, "plans", "new-plan.md"));

    indexContent = fs.readFileSync(
      path.join(docsDir, "plans", "index.md"),
      "utf8"
    );
    assert.doesNotMatch(indexContent, /\.\/new-plan\.md/);
    assert.match(indexContent, /- \[Live Sidebar\]\(\.\/live-sidebar\.md\)/);
  });
