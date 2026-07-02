import {defineDocsProject} from "@gabrielwawerski/docboard";

const projectDocsConfig = defineDocsProject({
  title: "Potion Panic",
  description: "Shared project docs and task board for Potion Panic.",
  base: "/PotionPanic/",
  docsRoot: "Docs",
  pagePathPrefix: "Docs",
  nav: [
    {text: "Home", link: "/"},
    {text: "Board", link: "/board"},
    {
      text: "Work",
      items: [
        {text: "Plans", link: "/plans/"},
        {text: "Milestones", link: "/milestones/"},
      ],
    },
    {
      text: "Project",
      items: [
        {text: "Game Design", link: "/project/game-design"},
        {text: "MVP Scope", link: "/project/mvp-scope"},
        {
          text: "Technical Architecture",
          link: "/project/technical-architecture",
        },
      ],
    },
    {
      text: "Guides",
      items: [
        {text: "Guides", link: "/guides/unity/"},
        {text: "Runtime Architecture", link: "/guides/unity/runtime-architecture"},
        {
          text: "Coding And Implementation",
          link: "/guides/unity/coding-and-implementation",
        },
        {text: "Editor Safety", link: "/guides/unity/editor-safety"},
        {
          text: "Presentation Workflows",
          link: "/guides/unity/presentation-workflows",
        },
      ],
    },
    {
      text: "Handbook",
      items: [
        {text: "Getting Started", link: "/onboarding/getting-started"},
        {text: "Workflow", link: "/collaboration/team-workflow"},
      ],
    },
    {
      text: "Archive",
      items: [
        {text: "Archive", link: "/archive/"},
        {text: "Archive Board", link: "/archive/board"},
        {text: "Archived Plans", link: "/archive/completed/"},
      ],
    },
  ],
  socialLinks: [
    {icon: "github", link: "https://github.com/gabrielwawerski/PotionPanic"},
  ],
  themeConfig: {
    outline: [2, 3],
  },
  sidebar: {
    excludedDirs: [
      ".vitepress",
      "archive/completed",
      "archive/tickets",
      "tickets",
    ],
    sections: [
      {
        text: "Start Here",
        items: [
          {text: "Docs Home", link: "/"},
          {text: "Getting Started", link: "/onboarding/getting-started"},
          {text: "Team Workflow", link: "/collaboration/team-workflow"},
        ],
      },
      {
        text: "Active Work",
        includeDirs: ["plans", "milestones"],
        items: [
          {text: "Board", link: "/board"},
          {text: "Implementation Plans", link: "/plans/"},
          {text: "Milestones", link: "/milestones/"},
        ],
      },
      {
        text: "Project Truth",
        items: [
          {text: "Game Design", link: "/project/game-design"},
          {text: "MVP Scope", link: "/project/mvp-scope"},
          {
            text: "Technical Architecture",
            link: "/project/technical-architecture",
          },
          {
            text: "Game Design And Psychology",
            link: "/project/game-design-and-psychology",
          },
        ],
      },
      {
        text: "Unity Guides",
        items: [
          {text: "Guides", link: "/guides/unity/"},
          {
            text: "Runtime Architecture",
            link: "/guides/unity/runtime-architecture",
          },
          {
            text: "Coding And Implementation",
            link: "/guides/unity/coding-and-implementation",
          },
          {text: "Editor Safety", link: "/guides/unity/editor-safety"},
          {
            text: "Presentation Workflows",
            link: "/guides/unity/presentation-workflows",
          },
        ],
      },
      {
        text: "Archive",
        items: [
          {text: "Archive", link: "/archive/"},
          {text: "Archive Board", link: "/archive/board"},
          {text: "Archived Plans", link: "/archive/completed/"},
        ],
      },
    ],
  },
  plans: {
    activeDir: "plans",
    activeIndex: "plans/index.md",
    archiveDir: "archive/completed",
    archiveIndex: "archive/completed/index.md",
  },
});

export default projectDocsConfig;
