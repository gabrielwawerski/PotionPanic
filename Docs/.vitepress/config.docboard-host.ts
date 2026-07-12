import {fileURLToPath} from "node:url";

import {createDocsConfig} from "@gabrielwawerski/docboard";

import projectDocsConfig from "./project-docs.config";

const docsDir = fileURLToPath(new URL("..", import.meta.url));

export default createDocsConfig(projectDocsConfig, {docsDir});
