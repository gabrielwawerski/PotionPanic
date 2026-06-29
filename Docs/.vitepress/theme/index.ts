import type {Theme} from "vitepress";
import DefaultTheme from "vitepress/theme";

import {applySidebarThemeUpdate} from "../lib/sidebar-site-data.mjs";
import Layout from "./Layout.vue";
import "./styles/board.css";

const theme: Theme = {
  extends: DefaultTheme,
  enhanceApp({siteData}) {
    if (!import.meta.hot) {
      return;
    }

    import.meta.hot.on("potion-panic:sidebar-update", (payload) => {
      if (!payload?.sidebar) {
        return;
      }

      siteData.value = applySidebarThemeUpdate(siteData.value, payload.sidebar);
    });
  },
  Layout,
};

export default theme;
