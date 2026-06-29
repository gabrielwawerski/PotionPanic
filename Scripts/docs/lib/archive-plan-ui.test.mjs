import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const layoutPath = path.resolve("Docs/.vitepress/theme/Layout.vue");
const componentPath = path.resolve(
  "Docs/.vitepress/theme/components/ArchivePlanButton.vue"
);
const composablePath = path.resolve(
  "Docs/.vitepress/theme/composables/usePlanArchive.ts"
);

test("layout renders the archive plan control in doc-footer-before", () => {
  const source = fs.readFileSync(layoutPath, "utf8");

  assert.match(source,
    /<template #doc-footer-before>\s*<ArchivePlanButton \/>[\s\S]*?<\/template>/);
});

test("archive plan button posts to the archive plan endpoint", () => {
  const componentSource = fs.readFileSync(componentPath, "utf8");
  const composableSource = fs.readFileSync(composablePath, "utf8");

  assert.match(componentSource, /window\.confirm/);
  assert.match(componentSource, /router\.go/);
  assert.match(composableSource, /__vitepress_pm_archive_plan/);
});
