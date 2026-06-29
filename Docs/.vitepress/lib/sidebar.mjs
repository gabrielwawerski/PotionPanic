import fs from "node:fs";
import path from "node:path";

import matter from "gray-matter";

const DEFAULT_EXCLUDED_DIRS = [
  ".vitepress",
  "archive/completed",
  "tickets",
];

function normalizePath(value) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/").replace(/^\/+|\/+$/g, "");
}

function titleCaseFromSlug(value) {
  return value
  .split(/[-_\s]+/)
  .filter(Boolean)
  .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
  .join(" ");
}

function isExcludedPath(relativePath, excludedDirs) {
  return excludedDirs.some((dir) => (
    relativePath === dir || relativePath.startsWith(`${dir}/`)
  ));
}

function findMarkdownFiles(docsDir, startDir, excludedDirs) {
  const normalizedStartDir = normalizePath(startDir);
  const baseDir = normalizedStartDir
    ? path.resolve(docsDir, normalizedStartDir)
    : path.resolve(docsDir);

  if (!fs.existsSync(baseDir)) {
    return [];
  }

  const results = [];

  function visit(currentDir) {
    for (const entry of fs.readdirSync(currentDir, {withFileTypes: true})) {
      const fullPath = path.join(currentDir, entry.name);
      const relativePath = normalizePath(path.relative(docsDir, fullPath));

      if (!relativePath) {
        continue;
      }

      if (entry.isDirectory()) {
        if (
          entry.name.startsWith(".")
          || entry.name === "node_modules"
          || isExcludedPath(relativePath, excludedDirs)
        ) {
          continue;
        }

        visit(fullPath);
        continue;
      }

      if (
        entry.isFile()
        && entry.name.endsWith(".md")
        && !isExcludedPath(relativePath, excludedDirs)
      ) {
        results.push(relativePath);
      }
    }
  }

  visit(baseDir);

  return results;
}

function extractHeading(content) {
  const match = content.match(/^#\s+(.+?)\s*$/m);
  return match?.[1]?.trim() || "";
}

function buildSidebarLink(relativePath) {
  const normalized = normalizePath(relativePath);

  if (!normalized.endsWith(".md")) {
    return null;
  }

  if (normalized === "index.md") {
    return "/";
  }

  if (normalized.endsWith("/index.md")) {
    return `/${normalized.slice(0, -"index.md".length)}`;
  }

  return `/${normalized.replace(/\.md$/i, "")}`;
}

function readSidebarItem(docsDir, relativePath) {
  const filePath = path.resolve(docsDir, relativePath);
  const raw = fs.readFileSync(filePath, "utf8");
  const parsed = matter(raw);

  if (parsed.data.sidebar === false) {
    return null;
  }

  const link = buildSidebarLink(relativePath);
  if (!link) {
    return null;
  }

  const baseName = path.basename(relativePath, ".md");
  const label = `${parsed.data.title ?? ""}`.trim()
    || extractHeading(parsed.content)
    || titleCaseFromSlug(baseName);

  return {
    text: label,
    link,
    isIndexPage: baseName === "index",
  };
}

function compareSidebarItems(left, right) {
  if (left.isIndexPage !== right.isIndexPage) {
    return left.isIndexPage ? -1 : 1;
  }

  const labelOrder = left.text.localeCompare(right.text, undefined, {
    sensitivity: "base",
  });

  if (labelOrder !== 0) {
    return labelOrder;
  }

  return left.link.localeCompare(right.link, undefined, {
    sensitivity: "base",
  });
}

function buildSectionItems(docsDir, section, excludedDirs) {
  const manualItems = Array.isArray(section.items) ? [...section.items] : [];
  const includeDirs = Array.isArray(section.includeDirs) ? section.includeDirs : [];
  const seenLinks = new Set(manualItems.map((item) => item.link));
  const autoItems = [];

  for (const includeDir of includeDirs) {
    for (const relativePath of findMarkdownFiles(docsDir, includeDir, excludedDirs)) {
      const item = readSidebarItem(docsDir, relativePath);

      if (!item || seenLinks.has(item.link)) {
        continue;
      }

      seenLinks.add(item.link);
      autoItems.push(item);
    }
  }

  autoItems.sort(compareSidebarItems);

  return [
    ...manualItems,
    ...autoItems.map(({text, link}) => ({text, link})),
  ];
}

export function buildSidebar({
  docsDir,
  excludedDirs = DEFAULT_EXCLUDED_DIRS,
  sections = [],
} = {}) {
  return sections.map((section) => ({
    text: section.text,
    items: buildSectionItems(
      path.resolve(docsDir),
      section,
      excludedDirs.map(normalizePath)
    ),
  }));
}
