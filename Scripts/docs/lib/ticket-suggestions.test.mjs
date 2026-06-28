import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

import {
  buildTicketSuggestionCatalog,
  findAffectedFileSuggestionPaths,
  findDocumentationSuggestionPaths,
  normalizeBoardSuggestionConfig,
} from "../../../Docs/.vitepress/lib/ticket-suggestions.mjs";

test("normalizeBoardSuggestionConfig trims list seeds and drops empty values", () => {
  const config = normalizeBoardSuggestionConfig({
    assignees: [" Gabriel ", "", null, "Aga"],
    tags: [" combat ", "", null, "boss"],
    milestones: [" m-0 ", "m-1", ""],
    dependencies: [" PP-2 ", "PP-4", ""],
    documentation: [" project/mvp-scope.md ", "", "README.md"],
    affectedFiles: [" Assets/Scenes/Laboratory.unity ", "", "README.md"],
  });

  assert.deepEqual(config, {
    assignees: ["Gabriel", "Aga"],
    tags: ["combat", "boss"],
    milestones: ["m-0", "m-1"],
    dependencies: ["PP-2", "PP-4"],
    documentation: ["project/mvp-scope.md", "README.md"],
    affectedFiles: ["Assets/Scenes/Laboratory.unity", "README.md"],
  });
});

test("findDocumentationSuggestionPaths returns known doc paths and excludes tickets", () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "pp-doc-suggestions-"));
  const srcDir = path.join(root, "Docs");
  const ticketsDir = path.join(srcDir, "tickets");
  const guidesDir = path.join(srcDir, "guides");

  fs.mkdirSync(ticketsDir, {recursive: true});
  fs.mkdirSync(guidesDir, {recursive: true});

  fs.writeFileSync(path.join(root, "README.md"), "# Root readme\n");
  fs.writeFileSync(path.join(srcDir, "board.md"), "# Board\n");
  fs.writeFileSync(path.join(guidesDir, "workflow.md"), "# Workflow\n");
  fs.writeFileSync(path.join(ticketsDir, "PP-2.md"), "# Ticket\n");

  const suggestions = findDocumentationSuggestionPaths(srcDir, "tickets");

  assert.deepEqual(suggestions, [
    "README.md",
    "board.md",
    "guides/workflow.md",
  ]);
});

test("findAffectedFileSuggestionPaths returns repo paths and excludes tickets", () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "pp-file-suggestions-"));
  const docsDir = path.join(root, "Docs");
  const ticketsDir = path.join(docsDir, "tickets");
  const sceneDir = path.join(root, "Assets", "Scenes");
  const libraryDir = path.join(root, "Library");

  fs.mkdirSync(ticketsDir, {recursive: true});
  fs.mkdirSync(sceneDir, {recursive: true});
  fs.mkdirSync(libraryDir, {recursive: true});

  fs.writeFileSync(path.join(root, "README.md"), "# Root readme\n");
  fs.writeFileSync(path.join(root, "package.json"), "{}\n");
  fs.writeFileSync(path.join(docsDir, "board.md"), "# Board\n");
  fs.writeFileSync(path.join(sceneDir, "Laboratory.unity"), "%YAML\n");
  fs.writeFileSync(path.join(ticketsDir, "PP-2.md"), "# Ticket\n");
  fs.writeFileSync(path.join(libraryDir, "cache.asset"), "ignore\n");

  const suggestions = findAffectedFileSuggestionPaths(
    root,
    path.join("Docs", "tickets")
  );

  assert.deepEqual(suggestions, [
    "Assets/Scenes/Laboratory.unity",
    "Docs/board.md",
    "package.json",
    "README.md",
  ]);
});

test("buildTicketSuggestionCatalog merges observed and seeded suggestions", () => {
  const catalog = buildTicketSuggestionCatalog({
    boardSuggestions: {
      assignees: ["Gabriel", "Patro"],
      tags: ["planning", "combat"],
      milestones: ["m-0", "m-2"],
      dependencies: ["PP-9"],
      documentation: ["README.md", "project/mvp-scope.md"],
      affectedFiles: ["Assets/Scenes/Laboratory.unity", "README.md"],
    },
    affectedFilePaths: [
      "Assets/Scenes/Laboratory.unity",
      "Docs/board.md",
    ],
    documentationPaths: [
      "README.md",
      "board.md",
      "project/mvp-scope.md",
    ],
    prefix: "PP",
    tickets: [
      {
        id: 2,
        assignee: "Gabriel",
        title: "Replace SampleScene with Laboratory milestone scene",
        tags: ["planning", "scene"],
        milestone: "m-0",
        dependencies: [],
        documentation: ["project/mvp-scope.md"],
        affectedFiles: ["ProjectSettings/EditorBuildSettings.asset"],
      },
      {
        id: 4,
        assignee: "Aga",
        title: "Validate Laboratory milestone and align scene-name docs",
        tags: ["docs", "scene"],
        milestone: "m-1",
        dependencies: ["PP-2"],
        documentation: ["guides/workflow.md"],
        affectedFiles: ["Docs/board.md"],
      },
    ],
  });

  assert.deepEqual(catalog.assignees, ["Aga", "Gabriel", "Patro"]);
  assert.deepEqual(catalog.tags, ["combat", "docs", "planning", "scene"]);
  assert.deepEqual(catalog.milestones, ["m-0", "m-1", "m-2"]);
  assert.deepEqual(catalog.documentation, [
    "README.md",
    "board.md",
    "guides/workflow.md",
    "project/mvp-scope.md",
  ]);
  assert.deepEqual(catalog.affectedFiles, [
    "Assets/Scenes/Laboratory.unity",
    "Docs/board.md",
    "ProjectSettings/EditorBuildSettings.asset",
    "README.md",
  ]);
  assert.deepEqual(catalog.dependencies, [
    {
      label: "PP-2 - Replace SampleScene with Laboratory milestone scene",
      value: "PP-2",
    },
    {
      label: "PP-4 - Validate Laboratory milestone and align scene-name docs",
      value: "PP-4",
    },
    {
      label: "PP-9",
      value: "PP-9",
    },
  ]);
});
