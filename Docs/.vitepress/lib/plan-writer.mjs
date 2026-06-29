import fs from "node:fs";
import path from "node:path";

import matter from "gray-matter";

import {buildPlanPageUrl} from "./plan-archive-page.mjs";
import {DEFAULT_PLAN_TEMPLATE} from "./plan-common.mjs";
import {comparePlanSidebarItems} from "./sidebar.mjs";

function normalizePath(value) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/").replace(/^\/+|\/+$/g, "");
}

function normalizeSiteUrl(value) {
  return `${value ?? ""}`.trim().replace(/[?#].*$/, "");
}

function trimBlankLines(value) {
  return `${value ?? ""}`.replace(/\r\n/g, "\n").replace(/^\n+|\n+$/g, "");
}

export function normalizeDateValue(value) {
  if (value instanceof Date && !Number.isNaN(value.getTime())) {
    return value.toISOString().slice(0, 10);
  }

  const text = `${value ?? ""}`.trim();
  if (!text) {
    return "";
  }

  const isoMatch = text.match(/^(\d{4}-\d{2}-\d{2})/);
  if (isoMatch) {
    return isoMatch[1];
  }

  const parsed = new Date(text);
  if (!Number.isNaN(parsed.getTime())) {
    return parsed.toISOString().slice(0, 10);
  }

  return text;
}

function extractHeading(content) {
  const match = `${content ?? ""}`.match(/^#\s+(.+?)\s*$/m);
  return match?.[1]?.trim() || "";
}

function replaceHeading(content, title) {
  const normalizedTitle = `${title ?? ""}`.trim();
  const normalizedBody = trimBlankLines(content);
  const nextBody = normalizedBody.replace(/^#\s+.+?\s*$/m, "").trimStart();
  return trimBlankLines([
    `# ${normalizedTitle}`,
    nextBody,
  ].filter(Boolean).join("\n\n"));
}

function slugifyTitle(value) {
  const slug = `${value ?? ""}`
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return slug || "plan";
}

function inferDateFromPath(relativePath) {
  const normalized = normalizePath(relativePath);
  const match = normalized.match(/(?:^|\/)(?:\d{4}-\d{2}-\d{2}-[^/]+|[^/]+-(\d{4}-\d{2}-\d{2}))\.md$/);
  if (!match) {
    return "";
  }

  if (match[1]) {
    return match[1];
  }

  const prefixMatch = normalized.match(/(?:^|\/)(\d{4}-\d{2}-\d{2})-[^/]+\.md$/);
  return prefixMatch?.[1] || "";
}

function titleCaseFromSlug(value) {
  return `${value ?? ""}`
    .split(/[-_\s]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
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

function ensureActivePlanRelativePath(relativePath) {
  const normalized = normalizePath(relativePath);
  if (!normalized.startsWith("plans/") || normalized === "plans/index.md") {
    throw new Error("Only active plan pages are supported");
  }

  return normalized;
}

function todayDate() {
  return new Date().toISOString().slice(0, 10);
}

function findUniquePlanSlug(docsDir, slug) {
  let attempt = slugifyTitle(slug);
  let suffix = 2;

  while (fs.existsSync(path.join(docsDir, "plans", `${attempt}.md`))) {
    attempt = `${slugifyTitle(slug)}-${suffix}`;
    suffix += 1;
  }

  return attempt;
}

function readPlanMarkdown(filePath) {
  const raw = fs.readFileSync(filePath, "utf8");
  const parsed = matter(raw);
  const title = `${parsed.data.title ?? ""}`.trim() || extractHeading(parsed.content);
  const body = trimBlankLines(parsed.content.replace(/^#\s+.+?\s*$/m, ""));
  const date = normalizeDateValue(parsed.data.date);

  return {
    body,
    parsed,
    raw,
    title,
    date,
  };
}

function buildPlanFileContent({body, date, frontmatter = {}, title}) {
  const nextData = {...frontmatter};
  if (date) {
    nextData.date = date;
  }
  const nextContent = `${replaceHeading(body, title)}\n`;
  return matter.stringify(nextContent, nextData)
    .replace(/^date: ['"](\d{4}-\d{2}-\d{2})['"]$/m, "date: $1");
}

function findSectionRange(lines, heading) {
  const headingIndex = lines.findIndex((line) => line.trim() === heading);
  if (headingIndex === -1) {
    return {endIndex: lines.length, headingIndex: -1};
  }

  let endIndex = lines.length;
  for (let index = headingIndex + 1; index < lines.length; index += 1) {
    if (/^##\s+/.test(lines[index])) {
      endIndex = index;
      break;
    }
  }

  return {endIndex, headingIndex};
}

function rebuildActivePlansIndexContent(existingContent, bullets) {
  const heading = "## Active Plans";
  const placeholder = "_No active plans yet._";
  const lines = existingContent.split(/\r?\n/);
  const {endIndex, headingIndex} = findSectionRange(lines, heading);

  if (headingIndex === -1) {
    const trimmed = existingContent.trimEnd();
    return `${trimmed}\n\n${heading}\n\n${
      (bullets.length > 0 ? bullets : [placeholder]).join("\n")
    }\n`;
  }

  const rebuiltSection = bullets.length > 0
    ? bullets
    : [placeholder];
  const rebuilt = [
    ...lines.slice(0, headingIndex + 1),
    "",
    ...rebuiltSection,
    ...lines.slice(endIndex),
  ];

  return `${rebuilt.join("\n").replace(/\n{3,}/g, "\n\n").trimEnd()}\n`;
}

function listActivePlanEntries(docsDir) {
  const plansDir = path.join(docsDir, "plans");
  if (!fs.existsSync(plansDir)) {
    return [];
  }

  return fs.readdirSync(plansDir)
    .filter((file) => file.endsWith(".md") && file !== "index.md")
    .map((file) => {
      const filePath = path.join(plansDir, file);
      const raw = fs.readFileSync(filePath, "utf8");
      const parsed = matter(raw);
      const baseName = path.basename(file, ".md");

      return {
        date: normalizeDateValue(parsed.data.date),
        fileName: baseName,
        isIndexPage: false,
        link: buildPlanPageUrl(path.posix.join("plans", file)),
        text: `${parsed.data.title ?? ""}`.trim()
          || extractHeading(parsed.content)
          || titleCaseFromSlug(baseName),
      };
    })
    .sort(comparePlanSidebarItems);
}

export function syncActivePlansIndex(docsDir) {
  const indexPath = ensurePlansIndexExists(docsDir);
  const existingContent = fs.readFileSync(indexPath, "utf8");
  const entries = listActivePlanEntries(docsDir);
  const bullets = entries.map((entry) => (
    `- [${entry.text}](./${entry.fileName}.md)`
  ));
  const nextContent = rebuildActivePlansIndexContent(existingContent, bullets);

  if (nextContent !== existingContent) {
    fs.writeFileSync(indexPath, nextContent);
    return true;
  }

  return false;
}

function ensurePlansIndexExists(docsDir) {
  const indexPath = path.join(docsDir, "plans", "index.md");
  if (!fs.existsSync(indexPath)) {
    fs.mkdirSync(path.dirname(indexPath), {recursive: true});
    fs.writeFileSync(indexPath, [
      "# Implementation Plans",
      "",
      "## Active Plans",
      "",
      "_No active plans yet._",
      "",
    ].join("\n"));
  }

  return indexPath;
}

export function updateActivePlansIndex(docsDir, {fileName, mode = "upsert", title}) {
  void fileName;
  void mode;
  void title;
  syncActivePlansIndex(docsDir);
}

export function readPlanFile(docsDir, {url} = {}) {
  const relativePath = ensureActivePlanRelativePath(siteUrlToRelativePath(url));
  const {resolved} = resolveDocsPath(docsDir, relativePath);
  if (!fs.existsSync(resolved)) {
    throw new Error(`File not found: ${relativePath}`);
  }

  const {body, date, title} = readPlanMarkdown(resolved);

  return {
    body,
    date,
    filePath: relativePath,
    title,
    url: buildPlanPageUrl(relativePath),
  };
}

export function createPlanFile(docsDir, {body = "", title = "New Plan", today} = {}) {
  const normalizedTitle = `${title ?? ""}`.trim();
  if (!normalizedTitle) {
    throw new Error("Missing title");
  }

  const slug = findUniquePlanSlug(docsDir, normalizedTitle);
  const relativePath = `plans/${slug}.md`;
  const {resolved} = resolveDocsPath(docsDir, relativePath);
  const date = `${today ?? todayDate()}`.trim();

  fs.mkdirSync(path.dirname(resolved), {recursive: true});
  fs.writeFileSync(resolved, buildPlanFileContent({
    body: trimBlankLines(body) || DEFAULT_PLAN_TEMPLATE,
    date,
    title: normalizedTitle,
  }));
  updateActivePlansIndex(docsDir, {
    fileName: path.basename(resolved),
    title: normalizedTitle,
  });

  return {
    body: trimBlankLines(body) || DEFAULT_PLAN_TEMPLATE,
    date,
    filePath: relativePath,
    title: normalizedTitle,
    url: buildPlanPageUrl(relativePath),
  };
}

export function updatePlanFile(docsDir, {body = "", title, url} = {}) {
  const relativePath = ensureActivePlanRelativePath(siteUrlToRelativePath(url));
  const {resolved} = resolveDocsPath(docsDir, relativePath);
  if (!fs.existsSync(resolved)) {
    throw new Error(`File not found: ${relativePath}`);
  }

  const {parsed, date, title: existingTitle} = readPlanMarkdown(resolved);
  const normalizedTitle = `${title ?? ""}`.trim() || existingTitle;
  if (!normalizedTitle) {
    throw new Error("Missing title");
  }

  fs.writeFileSync(resolved, buildPlanFileContent({
    body: trimBlankLines(body),
    date,
    frontmatter: parsed.data,
    title: normalizedTitle,
  }));
  updateActivePlansIndex(docsDir, {
    fileName: path.basename(resolved),
    title: normalizedTitle,
  });

  return {
    body: trimBlankLines(body),
    date,
    filePath: relativePath,
    title: normalizedTitle,
    url: buildPlanPageUrl(relativePath),
  };
}

export function backfillPlanDates(docsDir, {fallbackDatesByPath = {}} = {}) {
  const plansDir = path.join(docsDir, "plans");
  if (!fs.existsSync(plansDir)) {
    return [];
  }

  const files = fs.readdirSync(plansDir)
    .filter((file) => file.endsWith(".md") && file !== "index.md");
  const results = [];

  for (const file of files) {
    const relativePath = normalizePath(path.posix.join("plans", file));
    const filePath = path.join(plansDir, file);
    const raw = fs.readFileSync(filePath, "utf8");
    const parsed = matter(raw);
    const existingDate = normalizeDateValue(parsed.data.date);
    if (existingDate) {
      continue;
    }

    const date = `${fallbackDatesByPath[relativePath] ?? inferDateFromPath(relativePath)}`.trim();
    if (!date) {
      throw new Error(`Missing fallback date for ${relativePath}`);
    }

    parsed.data.date = date;
    fs.writeFileSync(
      filePath,
      matter.stringify(parsed.content, parsed.data)
        .replace(/^date: ['"](\d{4}-\d{2}-\d{2})['"]$/m, "date: $1")
    );
    results.push({date, relativePath});
  }

  return results;
}

export {
  buildPlanPageUrl,
  inferDateFromPath,
  siteUrlToRelativePath,
};
