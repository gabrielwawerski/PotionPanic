import fs from "node:fs";
import path from "node:path";

import matter from "gray-matter";
import {
  buildPlanPageUrl,
  isArchivablePlanPage,
} from "./plan-archive-page.mjs";

function normalizePath(value) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/").replace(/^\/+|\/+$/g, "");
}

function normalizeSiteUrl(value) {
  return `${value ?? ""}`.trim().replace(/[?#].*$/, "");
}

function titleCaseFromSlug(value) {
  return value
  .split(/[-_\s]+/)
  .filter(Boolean)
  .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
  .join(" ");
}

function extractHeading(content) {
  const match = content.match(/^#\s+(.+?)\s*$/m);
  return match?.[1]?.trim() || "";
}

function siteUrlToRelativePath(url) {
  const normalizedUrl = normalizeSiteUrl(url);
  if (!normalizedUrl) {
    throw new Error("Missing url");
  }

  const normalizedPath = normalizedUrl.replace(/^\/+/, "");
  if (!normalizedPath) {
    throw new Error("Missing url");
  }

  if (normalizedPath.endsWith(".md")) {
    return normalizePath(normalizedPath);
  }

  if (normalizedPath.endsWith(".html")) {
    return normalizePath(normalizedPath.replace(/\.html$/i, ".md"));
  }

  if (normalizedPath.endsWith("/")) {
    return normalizePath(`${normalizedPath}index.md`);
  }

  return normalizePath(`${normalizedPath}.md`);
}

function resolveDocsPath(docsDir, relativePath) {
  const normalized = normalizePath(relativePath);
  const resolved = path.resolve(docsDir, normalized);
  const relative = path.relative(docsDir, resolved);
  if (relative.startsWith("..") || path.isAbsolute(relative)) {
    throw new Error(`Path escapes docs root: ${relativePath}`);
  }

  return {
    normalized,
    resolved,
  };
}

function buildArchivedPlansIndexContent(existingContent, bullet) {
  const heading = "## Archived Plans";
  const placeholder = "_No archived plans yet._";
  const lines = existingContent.split(/\r?\n/);
  const headingIndex = lines.findIndex((line) => line.trim() === heading);

  if (headingIndex === -1) {
    const trimmed = existingContent.trimEnd();
    return `${trimmed}\n\n${heading}\n\n${bullet}\n`;
  }

  let endIndex = lines.length;
  for (let index = headingIndex + 1; index < lines.length; index += 1) {
    if (/^##\s+/.test(lines[index])) {
      endIndex = index;
      break;
    }
  }

  const sectionEntries = lines
    .slice(headingIndex + 1, endIndex)
    .filter((line) => line.trim() !== "" && line.trim() !== placeholder);

  if (!sectionEntries.includes(bullet)) {
    sectionEntries.push(bullet);
  }

  const rebuilt = [
    ...lines.slice(0, headingIndex + 1),
    "",
    ...sectionEntries,
    ...lines.slice(endIndex),
  ];

  return `${rebuilt.join("\n").replace(/\n{3,}/g, "\n\n").trimEnd()}\n`;
}

function updateArchivedPlansIndex(docsDir, {fileName, title}) {
  const {resolved: indexPath} = resolveDocsPath(
    docsDir,
    "archive/completed/index.md"
  );
  const bullet = `- [${title}](./${fileName})`;
  const existingContent = fs.existsSync(indexPath)
    ? fs.readFileSync(indexPath, "utf8")
    : [
      "# Archived Plans",
      "",
      "Completed or superseded implementation plans from `Docs/plans/` live here.",
      "",
      "## Archived Plans",
      "",
      "_No archived plans yet._",
      "",
    ].join("\n");

  const nextContent = buildArchivedPlansIndexContent(existingContent, bullet);
  fs.mkdirSync(path.dirname(indexPath), {recursive: true});
  fs.writeFileSync(indexPath, nextContent);
}

export {buildPlanPageUrl, isArchivablePlanPage} from "./plan-archive-page.mjs";

export function archivePlanFile(docsDir, {url} = {}) {
  const sourceRelativePath = siteUrlToRelativePath(url);
  if (!isArchivablePlanPage(sourceRelativePath)) {
    throw new Error("Only non-index plan pages can be archived");
  }

  const {resolved: sourcePath} = resolveDocsPath(docsDir, sourceRelativePath);
  if (!fs.existsSync(sourcePath)) {
    throw new Error(`File not found: ${sourceRelativePath}`);
  }

  const fileName = path.basename(sourcePath);
  const {
    normalized: targetDirRelative,
    resolved: targetDirPath,
  } = resolveDocsPath(docsDir, "archive/completed");
  const targetPath = path.join(targetDirPath, fileName);

  if (fs.existsSync(targetPath)) {
    throw new Error(`Target already exists: ${targetDirRelative}/${fileName}`);
  }

  const raw = fs.readFileSync(sourcePath, "utf8");
  const parsed = matter(raw);
  const title = `${parsed.data.title ?? ""}`.trim()
    || extractHeading(parsed.content)
    || titleCaseFromSlug(path.basename(fileName, ".md"));

  fs.mkdirSync(targetDirPath, {recursive: true});
  fs.writeFileSync(targetPath, raw);
  fs.unlinkSync(sourcePath);
  updateArchivedPlansIndex(docsDir, {fileName, title});

  return {
    title,
    url: buildPlanPageUrl(path.posix.join(targetDirRelative, fileName)),
  };
}
