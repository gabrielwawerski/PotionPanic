import assert from "node:assert/strict";
import test from "node:test";

import fs from "node:fs";
import path from "node:path";

import docsConfig from "../../../Docs/.vitepress/config.mts";

test("PotionPanic managed docs config exposes the current navigation contract", () => {
  assert.equal(docsConfig.title, "Potion Panic");
  assert.deepEqual(
    docsConfig.themeConfig.nav.map(({text}) => text),
    ["Home", "Board", "Work", "Project", "Guides", "Archive"]
  );
  const navigationLinks = collectNavigationLinks(docsConfig.themeConfig.nav);
  for (const requiredRoute of [
    "/onboarding/getting-started",
    "/collaboration/team-workflow",
    "/project/",
    "/guides/",
    "/guides/coordinated-leasing",
    "/guides/unity/",
    "/research/game-design-and-psychology",
  ]) {
    assert.ok(
      navigationLinks.includes(requiredRoute),
      `expected navigation to include ${requiredRoute}`
    );
  }
  assert.ok(
    navigationLinks.every((link) => !link.startsWith("/unity-guides")),
    "navigation should use the /guides/unity/ public hierarchy"
  );
  assert.equal(docsConfig.lastUpdated, true);
  assert.deepEqual(docsConfig.themeConfig.outline, [2, 3]);
  assert.deepEqual(docsConfig.themeConfig.search, {provider: "local"});
  assert.ok(
    docsConfig.themeConfig.sidebar["/"].some(
      (section) => section.text === "Guides"
    ),
    "expected PotionPanic to keep the Guides sidebar section"
  );
  assert.ok(
    docsConfig.themeConfig.sidebar["/"].every(
      (section) => section.text !== "Unity Guides"
    ),
    "Unity guides should be nested under the Guides sidebar section"
  );
  assert.deepEqual(JSON.parse(docsConfig.vite.define.__DOCBOARD_THEME_OPTIONS__), {
    pagePathPrefix: "Docs",
    plans: {
      activeDir: "plans",
      activeIndex: "plans/index.md",
      archiveDir: "plans/archive",
      archiveIndex: "plans/archive/index.md",
    },
  });
});

function collectNavigationLinks(items) {
  return items.flatMap((item) => [
    ...(item.link ? [item.link] : []),
    ...(item.items ? collectNavigationLinks(item.items) : []),
  ]);
}

test("PotionPanic favicon links include the GitHub Pages base path", () => {
  assert.deepEqual(docsConfig.head, [
    ["link", {rel: "icon", type: "image/svg+xml", href: "/PotionPanic/favicon.svg"}],
    ["link", {rel: "icon", type: "image/x-icon", href: "/PotionPanic/favicon.ico"}],
    [
      "link",
      {
        rel: "icon",
        type: "image/png",
        sizes: "512x512",
        href: "/PotionPanic/logo.png",
      },
    ],
  ]);
});

test("PotionPanic theme entrypoint delegates to the Docboard package theme", () => {
  const themeEntrypoint = fs.readFileSync(
    path.resolve("Docs/.vitepress/theme/index.ts"),
    "utf8"
  );

  assert.equal(
    themeEntrypoint.trim(),
    '// Docboard managed theme v1\n' +
      'export {projectManagementTheme as default} from "@gabrielwawerski/docboard/theme";'
  );
});
