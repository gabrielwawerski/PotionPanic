// Docboard managed config wrapper v1
import {fileURLToPath} from "node:url";

import {defineDocsProject, withDocboardConfig} from "@gabrielwawerski/docboard";
import hostConfig from "./config.docboard-host.ts";

const docsDir = fileURLToPath(new URL("../", import.meta.url));

export default withDocboardConfig(hostConfig, defineDocsProject({
  title: "Project Docs",
  description: "Internal docs and planning",
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
