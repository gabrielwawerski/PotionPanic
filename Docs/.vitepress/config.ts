import {fileURLToPath} from "node:url";

import {defineConfig} from "vitepress";
import {markdownWriterPlugin} from "./lib/markdown-writer-plugin.mjs";
import {sidebarHmrPlugin} from "./lib/sidebar-hmr-plugin.mjs";
import {buildSidebarThemeConfig} from "./lib/sidebar.mjs";

const docsDir = fileURLToPath(new URL("..", import.meta.url));

export default defineConfig({
  title: "Potion Panic",
  description: "Shared project docs and task board for Potion Panic.",
  cleanUrls: true,
  vite: {
    plugins: [markdownWriterPlugin(), sidebarHmrPlugin()],
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
    sidebar: buildSidebarThemeConfig(docsDir),
    socialLinks: [
      {icon: "github", link: "https://github.com/gabrielwawerski/PotionPanic"},
    ],
  },
});
