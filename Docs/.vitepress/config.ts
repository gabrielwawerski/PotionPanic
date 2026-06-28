import {defineConfig} from "vitepress";
import {markdownWriterPlugin} from "./lib/markdown-writer-plugin.mjs";

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
            {text: "VitePress Board UX Plans", link: "/plans/vitepress-board-ux-plans"},
          ],
        },
        {
          text: "Guides",
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
          items: [
            {text: "Milestones", link: "/milestones/"},
            {text: "Archive", link: "/archive/"},
          ],
        },
      ],
    },
    socialLinks: [
      {icon: "github", link: "https://github.com/gabrielwawerski/PotionPanic"},
    ],
  },
});
