import fs from "node:fs";
import path from "node:path";

function normalizeEntry(value) {
  return `${value ?? ""}`.trim();
}

function normalizePath(value) {
  return normalizeEntry(value).replace(/\\/g, "/");
}

function normalizeUniqueList(values, normalize = normalizeEntry) {
  const seen = new Set();
  const results = [];

  for (const rawValue of values || []) {
    const value = normalize(rawValue);
    if (!value || seen.has(value)) {
      continue;
    }

    seen.add(value);
    results.push(value);
  }

  return results;
}

function sortStrings(values) {
  return [...values].sort((left, right) => (
    left.localeCompare(right, undefined, {sensitivity: "base"})
  ));
}

function sortDocumentationPaths(values) {
  return [...values].sort((left, right) => {
    if (left === "README.md" && right !== "README.md") {
      return -1;
    }

    if (right === "README.md" && left !== "README.md") {
      return 1;
    }

    return left.localeCompare(right, undefined, {sensitivity: "base"});
  });
}

function toTicketReference(id, prefix = "") {
  return prefix ? `${prefix}-${id}` : String(id);
}

function findMarkdownFiles(dir) {
  if (!fs.existsSync(dir)) {
    return [];
  }

  const results = [];

  for (const entry of fs.readdirSync(dir, {withFileTypes: true})) {
    const fullPath = path.join(dir, entry.name);

    if (
      entry.isDirectory()
      && !entry.name.startsWith(".")
      && entry.name !== "node_modules"
    ) {
      results.push(...findMarkdownFiles(fullPath));
    }
    else if (entry.isFile() && entry.name.endsWith(".md")) {
      results.push(fullPath);
    }
  }

  return results;
}

const IGNORED_REPO_DIRS = new Set([
  ".git",
  ".idea",
  "Library",
  "Logs",
  "node_modules",
  "obj",
  "Temp",
  "UserSettings",
]);

const INCLUDED_HIDDEN_REPO_DIRS = new Set([
  ".vitepress",
]);

function findRepoFiles(dir) {
  if (!fs.existsSync(dir)) {
    return [];
  }

  const results = [];

  for (const entry of fs.readdirSync(dir, {withFileTypes: true})) {
    const fullPath = path.join(dir, entry.name);

    if (entry.isDirectory()) {
      if (
        IGNORED_REPO_DIRS.has(entry.name)
        || (
          entry.name.startsWith(".")
          && !INCLUDED_HIDDEN_REPO_DIRS.has(entry.name)
        )
      ) {
        continue;
      }

      results.push(...findRepoFiles(fullPath));
      continue;
    }

    if (entry.isFile()) {
      results.push(fullPath);
    }
  }

  return results;
}

export function normalizeBoardSuggestionConfig(source = {}) {
  return {
    assignees: normalizeUniqueList(source.assignees),
    tags: normalizeUniqueList(source.tags),
    milestones: normalizeUniqueList(source.milestones),
    dependencies: normalizeUniqueList(source.dependencies),
    documentation: normalizeUniqueList(source.documentation),
    affectedFiles: normalizeUniqueList(source.affectedFiles, normalizePath),
  };
}

export function findDocumentationSuggestionPaths(srcDir, ticketsDir = "tickets") {
  const normalizedTicketsDir = normalizeEntry(ticketsDir).replace(/\\/g, "/")
  .replace(/^\/+|\/+$/g, "");
  const markdownFiles = findMarkdownFiles(srcDir);
  const suggestions = [];

  const rootReadme = path.resolve(srcDir, "..", "README.md");
  if (fs.existsSync(rootReadme)) {
    suggestions.push("README.md");
  }

  for (const file of markdownFiles) {
    const relative = path.relative(srcDir, file).replace(/\\/g, "/");

    if (
      normalizedTicketsDir
      && (relative === normalizedTicketsDir
        || relative.startsWith(`${normalizedTicketsDir}/`))
    ) {
      continue;
    }

    suggestions.push(relative);
  }

  return sortDocumentationPaths(normalizeUniqueList(suggestions));
}

export function findAffectedFileSuggestionPaths(repoRoot, ticketsDir = "") {
  const resolvedRoot = path.resolve(repoRoot);
  const resolvedTicketsDir = ticketsDir
    ? path.resolve(resolvedRoot, ticketsDir)
    : "";
  const normalizedTicketsDir = resolvedTicketsDir
    ? normalizePath(path.relative(resolvedRoot, resolvedTicketsDir))
      .replace(/^\/+|\/+$/g, "")
    : "";
  const suggestions = [];

  for (const file of findRepoFiles(resolvedRoot)) {
    const relative = normalizePath(path.relative(resolvedRoot, file));

    if (
      normalizedTicketsDir
      && (relative === normalizedTicketsDir
        || relative.startsWith(`${normalizedTicketsDir}/`))
    ) {
      continue;
    }

    suggestions.push(relative);
  }

  return sortStrings(normalizeUniqueList(suggestions, normalizePath));
}

export function buildTicketSuggestionCatalog(
  {
    affectedFilePaths = [],
    boardSuggestions = {},
    dependencyTickets = [],
    documentationPaths = [],
    prefix = "",
    tickets = [],
  } = {}
) {
  const normalizedBoardSuggestions = normalizeBoardSuggestionConfig(
    boardSuggestions
  );

  const tags = sortStrings(normalizeUniqueList([
    ...normalizedBoardSuggestions.tags,
    ...tickets.flatMap((ticket) => ticket.tags || []),
  ]));

  const assignees = sortStrings(normalizeUniqueList([
    ...normalizedBoardSuggestions.assignees,
    ...tickets.map((ticket) => ticket.assignee),
  ]));

  const milestones = sortStrings(normalizeUniqueList([
    ...normalizedBoardSuggestions.milestones,
    ...tickets.map((ticket) => ticket.milestone),
  ]));

  const documentation = sortDocumentationPaths(normalizeUniqueList([
    ...normalizedBoardSuggestions.documentation,
    ...documentationPaths,
    ...tickets.flatMap((ticket) => ticket.documentation || []),
  ]));

  const affectedFiles = sortStrings(normalizeUniqueList([
    ...normalizedBoardSuggestions.affectedFiles,
    ...affectedFilePaths,
    ...tickets.flatMap((ticket) => ticket.affectedFiles || []),
  ], normalizePath));

  const dependencyMap = new Map();

  for (const ticket of dependencyTickets.length > 0 ? dependencyTickets : tickets) {
    if (!ticket || !(Number(ticket.id) > 0)) {
      continue;
    }

    const value = toTicketReference(Number(ticket.id), prefix);
    const title = normalizeEntry(ticket.title);
    const dependency = {
      label: title ? `${value} - ${title}` : value,
      value,
    };

    if (ticket.url) {
      dependency.url = ticket.url;
    }

    if (ticket.archivedAt) {
      dependency.archived = true;
    }

    dependencyMap.set(value, dependency);
  }

  for (const seededDependency of normalizedBoardSuggestions.dependencies) {
    if (!dependencyMap.has(seededDependency)) {
      dependencyMap.set(seededDependency, {
        label: seededDependency,
        value: seededDependency,
      });
    }
  }

  const dependencies = [...dependencyMap.values()].sort((left, right) => (
    left.value.localeCompare(right.value, undefined, {sensitivity: "base"})
  ));

  return {
    affectedFiles,
    assignees,
    dependencies,
    documentation,
    milestones,
    tags,
  };
}
