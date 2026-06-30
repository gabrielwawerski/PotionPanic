import {fileURLToPath} from "node:url";

import {defineConfig} from "vitepress";
import {markdownWriterPlugin} from "./lib/markdown-writer-plugin.mjs";
import {sidebarHmrPlugin} from "./lib/sidebar-hmr-plugin.mjs";
import {buildSidebarThemeConfig} from "./lib/sidebar.mjs";
import projectDocsConfig from "./project-docs.config";

const docsDir = fileURLToPath(new URL("..", import.meta.url));

export default defineConfig({
  title: projectDocsConfig.title,
  description: projectDocsConfig.description,
  base: projectDocsConfig.base,
  cleanUrls: true,
  vite: {
    plugins: [
      markdownWriterPlugin(),
      sidebarHmrPlugin(projectDocsConfig.sidebar),
    ],
  },
  themeConfig: {
    nav: projectDocsConfig.nav,
    sidebar: buildSidebarThemeConfig(docsDir, projectDocsConfig.sidebar),
    socialLinks: projectDocsConfig.socialLinks,
  },
});
