export function applySidebarThemeUpdate(siteData, sidebar) {
  return {
    ...siteData,
    themeConfig: {
      ...siteData.themeConfig,
      sidebar,
    },
  };
}
