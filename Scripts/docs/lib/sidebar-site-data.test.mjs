import assert from "node:assert/strict";
import test from "node:test";

async function loadSidebarSiteDataModule() {
  try {
    return await import("../../../Docs/.vitepress/lib/sidebar-site-data.mjs");
  } catch (error) {
    assert.fail(`sidebar site data module missing: ${error.message}`);
  }
}

test("applySidebarThemeUpdate replaces only themeConfig.sidebar", async () => {
  const {applySidebarThemeUpdate} = await loadSidebarSiteDataModule();
  const original = {
    title: "Potion Panic",
    description: "Docs",
    themeConfig: {
      nav: [{text: "Board", link: "/board"}],
      sidebar: {"/": [{text: "Old", items: []}]},
      socialLinks: [{icon: "github", link: "https://example.com"}],
    },
  };
  const nextSidebar = {
    "/": [{text: "New", items: [{text: "Overview", link: "/"}]}],
  };

  const updated = applySidebarThemeUpdate(original, nextSidebar);

  assert.notEqual(updated, original);
  assert.notEqual(updated.themeConfig, original.themeConfig);
  assert.deepEqual(updated.themeConfig.sidebar, nextSidebar);
  assert.deepEqual(updated.themeConfig.nav, original.themeConfig.nav);
  assert.deepEqual(updated.themeConfig.socialLinks,
    original.themeConfig.socialLinks);
});
