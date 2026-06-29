import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

import {buildProjectRootPagePath} from "../../../Docs/.vitepress/lib/page-path-copy.mjs";

const layoutSource = fs.readFileSync(
  path.resolve("Docs/.vitepress/theme/Layout.vue"),
  "utf8"
);

test("VitePress layout exposes a doc-before copy path action with copied feedback",
  () => {
    assert.match(
      layoutSource,
      /<template #doc-before>/,
      "expected the shared layout to render page actions above doc content"
    );
    assert.match(
      layoutSource,
      /Copy Path/,
      "expected a page path copy button label"
    );
    assert.match(
      layoutSource,
      /Copied/,
      "expected copied feedback in the button label"
    );
  });

test("buildProjectRootPagePath keeps the Docs prefix and markdown extension",
  () => {
    assert.equal(
      buildProjectRootPagePath("project/game-design.md"),
      "Docs/project/game-design.md"
    );
    assert.equal(
      buildProjectRootPagePath("plans\\vitepress-theme-review-2026-06-28.md"),
      "Docs/plans/vitepress-theme-review-2026-06-28.md"
    );
  });
