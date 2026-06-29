import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const layoutPath = path.resolve("Docs/.vitepress/theme/Layout.vue");
const componentPath = path.resolve(
  "Docs/.vitepress/theme/components/PlanAuthoringControls.vue"
);
const composablePath = path.resolve(
  "Docs/.vitepress/theme/composables/usePlanWriter.ts"
);
const archiveComposablePath = path.resolve(
  "Docs/.vitepress/theme/composables/usePlanArchive.ts"
);

test("layout renders the plan authoring controls in doc-before", () => {
  const source = fs.readFileSync(layoutPath, "utf8");

  assert.match(source,
    /<template #doc-before>[\s\S]*?<PlanAuthoringControls \/>[\s\S]*?<\/template>/);
  assert.doesNotMatch(source,
    /<template #doc-footer-before>\s*<PlanAuthoringControls \/>[\s\S]*?<\/template>/);
});

test("plan authoring controls wire create, update, and archive endpoints", () => {
  const componentSource = fs.readFileSync(componentPath, "utf8");
  const composableSource = fs.readFileSync(composablePath, "utf8");
  const archiveComposableSource = fs.readFileSync(archiveComposablePath, "utf8");

  assert.match(componentSource, /New Plan/);
  assert.match(componentSource, /Edit Plan/);
  assert.match(componentSource, /window\.confirm/);
  assert.match(componentSource, /router\.go/);
  assert.match(composableSource, /__vitepress_pm_create_plan/);
  assert.match(composableSource, /__vitepress_pm_update_plan/);
  assert.match(composableSource, /__vitepress_pm_plan/);
  assert.match(archiveComposableSource, /__vitepress_pm_archive_plan/);
});
