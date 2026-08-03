// Docboard managed config v1
import {fileURLToPath} from "node:url";

import {createDocsConfig, defineDocsProject} from "@gabrielwawerski/docboard";

const docsDir = fileURLToPath(new URL("..", import.meta.url));

export default createDocsConfig(defineDocsProject({
  head: [
    ["link", {rel: "icon", type: "image/png", sizes: "512x512", href: "/logo.png"}],
  ],
  themeConfig: {logo: "/logo.png"},
  title: "Potion Panic",
  description: "Shared project docs and task board for Potion Panic.",
  base: "/PotionPanic/",
  docsRoot: "Docs",
  pagePathPrefix: "Docs",
  nav: [
    {text: "Home", link: "/"},
    {text: "Board", link: "/board"},
    {text: "Plans", link: "/plans/"},
    {text: "Archive", link: "/archive/board"},
  ],
  plans: {
    activeDir: "plans",
    activeIndex: "plans/index.md",
    archiveDir: "plans/archive",
    archiveIndex: "plans/archive/index.md",
  },
  socialLinks: [
    {icon: "github", link: "https://github.com/gabrielwawerski/PotionPanic"},
  ],
}), {docsDir});
