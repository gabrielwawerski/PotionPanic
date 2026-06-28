import fs from "node:fs";
import path from "node:path";

function normalizeEntry(value) {
  return `${value ?? ""}`.trim();
}

function normalizeUniqueList(values) {
  const seen = new Set();
  const results = [];

  for (const rawValue of values || []) {
    const value = normalizeEntry(rawValue);
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

export function normalizeBoardSuggestionConfig(source = {}) {
  return {
    tags: normalizeUniqueList(source.tags),
    milestones: normalizeUniqueList(source.milestones),
    dependencies: normalizeUniqueList(source.dependencies),
    documentation: normalizeUniqueList(source.documentation),
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

export function buildTicketSuggestionCatalog(
  {
    boardSuggestions = {},
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

  const milestones = sortStrings(normalizeUniqueList([
    ...normalizedBoardSuggestions.milestones,
    ...tickets.map((ticket) => ticket.milestone),
  ]));

  const documentation = sortDocumentationPaths(normalizeUniqueList([
    ...normalizedBoardSuggestions.documentation,
    ...documentationPaths,
    ...tickets.flatMap((ticket) => ticket.documentation || []),
  ]));

  const dependencyMap = new Map();

  for (const ticket of tickets) {
    if (!ticket || !(Number(ticket.id) > 0)) {
      continue;
    }

    const value = toTicketReference(Number(ticket.id), prefix);
    const title = normalizeEntry(ticket.title);
    dependencyMap.set(value, {
      label: title ? `${value} - ${title}` : value,
      value,
    });
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
    dependencies,
    documentation,
    milestones,
    tags,
  };
}
