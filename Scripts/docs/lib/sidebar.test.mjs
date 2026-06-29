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

function writeMarkdown(root, relativePath, content) {
  const filePath = path.join(root, relativePath);
  fs.mkdirSync(path.dirname(filePath), {recursive: true});
  fs.writeFileSync(filePath, content);
}

test("buildSidebar keeps pinned items first and auto-includes eligible pages",
  async () => {
    const {buildSidebar} = await loadSidebarModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-"));

    writeMarkdown(docsDir, "index.md", "# Docs Index\n");
    writeMarkdown(docsDir, "board.md", "---\nboard: true\n---\n");
    writeMarkdown(docsDir, "collaboration/team-workflow.md",
      "# Team Workflow\n");
    writeMarkdown(docsDir,
      "collaboration/vitepress-project-management-feature-proposals.md",
      "# VitePress Project Management Feature Proposals\n");
    writeMarkdown(docsDir, "tickets/PP-7.md",
      "---\ntitle: Ticket Page\n---\n");
    writeMarkdown(docsDir, "archive/index.md", "# Archive\n");
    writeMarkdown(docsDir, "archive/completed/index.md", "# Archived Plans\n");
    writeMarkdown(docsDir, "archive/completed/pp-1.md",
      "---\ntitle: Completed Task\n---\n");
    writeMarkdown(docsDir, "archive/tickets/PP-9.md",
      "---\ntitle: Archived Ticket\n---\n");

    const sections = buildSidebar({
      docsDir,
      sections: [
        {
          text: "Docs",
          items: [
            {text: "Overview", link: "/"},
            {text: "Board", link: "/board"},
          ],
        },
        {
          text: "Collaboration",
          includeDirs: ["collaboration"],
          items: [
            {text: "Team Workflow", link: "/collaboration/team-workflow"},
          ],
        },
        {
          text: "Planning History",
          includeDirs: ["archive"],
          items: [
            {text: "Archive", link: "/archive/"},
            {text: "Archived Plans", link: "/archive/completed/"},
          ],
        },
      ],
    });

    assert.deepEqual(sections, [
      {
        text: "Docs",
        items: [
          {text: "Overview", link: "/"},
          {text: "Board", link: "/board"},
        ],
      },
      {
        text: "Collaboration",
        items: [
          {text: "Team Workflow", link: "/collaboration/team-workflow"},
          {
            text: "VitePress Project Management Feature Proposals",
            link: "/collaboration/vitepress-project-management-feature-proposals",
          },
        ],
      },
      {
        text: "Planning History",
        items: [
          {text: "Archive", link: "/archive/"},
          {text: "Archived Plans", link: "/archive/completed/"},
        ],
      },
    ]);
  });

test("buildSidebar sorts nested docs with index pages before sibling pages",
  async () => {
    const {buildSidebar} = await loadSidebarModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-"));

    writeMarkdown(docsDir, "guides/unity/runtime-architecture.md",
      "# Runtime Architecture\n");
    writeMarkdown(docsDir, "guides/unity/index.md", "# Unity Guides\n");
    writeMarkdown(docsDir, "guides/design/juice.md", "# Juice Guide\n");

    const sections = buildSidebar({
      docsDir,
      sections: [
        {
          text: "Guides",
          includeDirs: ["guides"],
          items: [],
        },
      ],
    });

    assert.deepEqual(sections, [
      {
        text: "Guides",
        items: [
          {text: "Unity Guides", link: "/guides/unity/"},
          {text: "Juice Guide", link: "/guides/design/juice"},
          {text: "Runtime Architecture", link: "/guides/unity/runtime-architecture"},
        ],
      },
    ]);
  });

test("buildSidebar derives labels from frontmatter title, heading, then filename",
  async () => {
    const {buildSidebar} = await loadSidebarModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-"));

    writeMarkdown(docsDir, "project/frontmatter-title.md",
      "---\ntitle: Frontmatter Title\n---\nBody only.\n");
    writeMarkdown(docsDir, "project/heading-only.md", "# Heading Only\n");
    writeMarkdown(docsDir, "project/fallback-label.md", "Body only.\n");

    const sections = buildSidebar({
      docsDir,
      sections: [
        {
          text: "Project",
          includeDirs: ["project"],
          items: [],
        },
      ],
    });

    assert.deepEqual(sections[0].items, [
      {text: "Fallback Label", link: "/project/fallback-label"},
      {text: "Frontmatter Title", link: "/project/frontmatter-title"},
      {text: "Heading Only", link: "/project/heading-only"},
    ]);
  });

test("buildSidebar respects sidebar false and default ignored paths",
  async () => {
    const {buildSidebar} = await loadSidebarModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-"));

    writeMarkdown(docsDir, "plans/index.md", "# Plans\n");
    writeMarkdown(docsDir, "plans/keep-me.md", "# Keep Me\n");
    writeMarkdown(docsDir, "plans/hidden.md",
      "---\nsidebar: false\n---\n# Hidden\n");
    writeMarkdown(docsDir, ".vitepress/ignored.md", "# Ignored\n");
    writeMarkdown(docsDir, "tickets/PP-2.md", "# Ticket\n");

    const sections = buildSidebar({
      docsDir,
      sections: [
        {
          text: "Plans",
          includeDirs: ["plans"],
          items: [],
        },
      ],
    });

    assert.deepEqual(sections, [
      {
        text: "Plans",
        items: [
          {text: "Plans", link: "/plans/"},
          {text: "Keep Me", link: "/plans/keep-me"},
        ],
      },
    ]);
  });

test("buildSidebar sorts plan pages by date with index first and newest last",
  async () => {
    const {buildSidebar} = await loadSidebarModule();
    const docsDir = fs.mkdtempSync(path.join(os.tmpdir(), "pp-sidebar-"));

    writeMarkdown(docsDir, "plans/index.md", "# Plans\n");
    writeMarkdown(docsDir, "plans/undated.md",
      "---\ndate: 2026-06-28\n---\n# Undated Label\n");
    writeMarkdown(docsDir, "plans/newer.md",
      "---\ndate: 2026-06-29\n---\n# Newer Plan\n");
    writeMarkdown(docsDir, "plans/older.md",
      "---\ndate: 2026-06-27\n---\n# Older Plan\n");

    const sections = buildSidebar({
      docsDir,
      sections: [
        {
          text: "Plans",
          includeDirs: ["plans"],
          items: [],
        },
      ],
    });

    assert.deepEqual(sections, [
      {
        text: "Plans",
        items: [
          {text: "Plans", link: "/plans/"},
          {text: "Older Plan", link: "/plans/older"},
          {text: "Undated Label", link: "/plans/undated"},
          {text: "Newer Plan", link: "/plans/newer"},
        ],
      },
    ]);
  });
