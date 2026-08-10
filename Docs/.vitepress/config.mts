// Docboard managed config v1
import {fileURLToPath} from "node:url";

import {createDocsConfig, defineDocsProject} from "@gabrielwawerski/docboard";

const docsDir = fileURLToPath(new URL("..", import.meta.url));

export default createDocsConfig(defineDocsProject({
  head: [
    ["link", {rel: "icon", type: "image/svg+xml", href: "/PotionPanic/favicon.svg"}],
    ["link", {rel: "icon", type: "image/x-icon", href: "/PotionPanic/favicon.ico"}],
    ["link", {rel: "icon", type: "image/png", sizes: "512x512", href: "/PotionPanic/logo.png"}],
  ],
  themeConfig: {logo: "/favicon.svg"},
  title: "Potion Panic",
  description: "Shared project docs and task board for Potion Panic.",
  socialPreview: {
    siteUrl: "https://gabrielwawerski.github.io/PotionPanic/",
  },
  base: "/PotionPanic/",
  docsRoot: "Docs",
  pagePathPrefix: "Docs",
  nav: [
    {text: "Home", link: "/"},
    {text: "Board", link: "/board"},
    {text: "Plans", link: "/plans/"},
    {
      text: "Project",
      items: [
        {text: "Project Overview", link: "/project/"},
        {text: "Game Design", link: "/project/game-design"},
        {text: "MVP Scope", link: "/project/mvp-scope"},
        {text: "Target Runtime Design", link: "/project/technical-architecture"},
      ],
    },
    {
      text: "Guides",
      items: [
        {text: "Guide Index", link: "/guides/"},
        {text: "Unity Coordination", link: "/guides/coordinated-leasing"},
        {text: "Unity Guides", link: "/guides/unity/"},
        {text: "Design Research", link: "/research/game-design-and-psychology"},
      ],
    },
    {text: "Archive", link: "/archive/"},
  ],
  sidebar: {
    autoDiscover: true,
    excludedDirs: ["chronicles", "archive"],
    sections: [
      {
        text: "Start Here",
        includeDirs: ["onboarding", "collaboration"],
        items: [
          {text: "Project Setup", link: "/onboarding/getting-started"},
          {text: "Daily Workflow", link: "/collaboration/team-workflow"},
        ],
      },
      {
        text: "Active Work",
        includeDirs: ["plans"],
        items: [{text: "Active Plans", link: "/plans/"}],
        sort: "modified",
      },
      {
        text: "Research",
        includeDirs: ["research"],
        items: [{
          text: "Design Research",
          link: "/research/game-design-and-psychology",
        }],
      },
      {
        text: "Guides",
        includeDirs: ["guides"],
        items: [{text: "Guide Index", link: "/guides/"}],
      },
      {
        text: "Project",
        includeDirs: ["project"],
        items: [{text: "Project Overview", link: "/project/"}],
      },
    ],
  },
  plans: {
    activeDir: "plans",
    activeIndex: "plans/index.md",
    archiveDir: "plans/archive",
    archiveIndex: "plans/archive/index.md",
  },
  socialLinks: [
    {icon: "github", link: "https://github.com/gabrielwawerski/PotionPanic"},
  ],
}), {docsDir});
