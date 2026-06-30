import assert from "node:assert/strict";
import test from "node:test";

import {
  createDocsConfig,
  defineDocsProject,
} from "@gabrielwawerski/docboard";
import fs from "node:fs";
import path from "node:path";

import projectDocsConfig from "../../../Docs/.vitepress/project-docs.config.ts";

test("PotionPanic docs config can be consumed by Docboard package APIs", () => {
  const project = defineDocsProject(projectDocsConfig);
  const config = createDocsConfig(project, {docsDir: "Docs"});

  assert.equal(config.title, "Potion Panic");
  assert.equal(config.themeConfig.nav, projectDocsConfig.nav);
  assert.ok(
    config.themeConfig.sidebar["/"].some(
      (section) => section.text === "Unity Guides"
    ),
    "expected PotionPanic to keep the Unity Guides sidebar section"
  );
  assert.deepEqual(JSON.parse(config.vite.define.__DOCBOARD_THEME_OPTIONS__), {
    pagePathPrefix: "Docs",
    plans: {
      activeDir: "plans",
      activeIndex: "plans/index.md",
      archiveDir: "archive/completed",
      archiveIndex: "archive/completed/index.md",
    },
  });
});

test("PotionPanic theme entrypoint delegates to the Docboard package theme", () => {
  const themeEntrypoint = fs.readFileSync(
    path.resolve("Docs/.vitepress/theme/index.ts"),
    "utf8"
  );

  assert.equal(
    themeEntrypoint.trim(),
    'export {projectManagementTheme as default} from "@gabrielwawerski/docboard/theme";'
  );
});
