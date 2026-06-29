import {fileURLToPath} from "node:url";

import {defineConfig} from "vitepress";
import {markdownWriterPlugin} from "./lib/markdown-writer-plugin.mjs";
import {buildSidebar} from "./lib/sidebar.mjs";

const docsDir = fileURLToPath(new URL("..", import.meta.url));

export default defineConfig({
  title: "Potion Panic",
  description: "Shared project docs and task board for Potion Panic.",
  cleanUrls: true,
  vite: {
    plugins: [markdownWriterPlugin()],
  },
  themeConfig: {
    nav: [
      {text: "Board", link: "/board"},
      {text: "Onboarding", link: "/onboarding/getting-started"},
      {text: "Workflow", link: "/collaboration/team-workflow"},
      {text: "Plans", link: "/plans/"},
      {text: "Project", link: "/project/game-design"},
      {text: "Archive", link: "/archive/"},
    ],
    sidebar: {
      "/": buildSidebar({
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
            text: "Onboarding",
            includeDirs: ["onboarding"],
            items: [
              {text: "Getting Started", link: "/onboarding/getting-started"},
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
            text: "Project",
            includeDirs: ["project"],
            items: [
              {text: "Game Design", link: "/project/game-design"},
              {text: "MVP Scope", link: "/project/mvp-scope"},
              {text: "Technical Architecture", link: "/project/technical-architecture"},
              {text: "Game Design and Psychology", link: "/project/game-design-and-psychology"},
            ],
          },
          {
            text: "Plans",
            includeDirs: ["plans"],
            items: [
              {text: "Implementation Plans", link: "/plans/"},
              {text: "VitePress Board UX Plans", link: "/plans/vitepress-board-ux-plans"},
            ],
          },
          {
            text: "Guides",
            includeDirs: ["guides"],
            items: [
              {text: "Unity Guides", link: "/guides/unity/"},
              {text: "Runtime Architecture", link: "/guides/unity/runtime-architecture"},
              {
                text: "Coding And Implementation",
                link: "/guides/unity/coding-and-implementation"
              },
              {text: "Editor Safety", link: "/guides/unity/editor-safety"},
              {
                text: "Presentation Workflows",
                link: "/guides/unity/presentation-workflows"
              },
            ],
          },
          {
            text: "Planning History",
            includeDirs: ["archive", "milestones"],
            items: [
              {text: "Milestones", link: "/milestones/"},
              {text: "Archive Board", link: "/archive/board"},
              {text: "Archive", link: "/archive/"},
            ],
          },
        ],
      }),
    },
    socialLinks: [
      {icon: "github", link: "https://github.com/gabrielwawerski/PotionPanic"},
    ],
  },
});
