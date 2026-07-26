// Docboard managed config v1
import {fileURLToPath} from "node:url";

import {createDocsConfig, defineDocsProject} from "@gabrielwawerski/docboard";

const docsDir = fileURLToPath(new URL("..", import.meta.url));

export default createDocsConfig(defineDocsProject({
  title: "Project Docs",
  description: "Internal docs and planning",
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
}), {docsDir});
