import path from "node:path";

import matter from "gray-matter";

const STATUS_MAP = new Map([
  ["Backlog", "backlog"],
  ["To Do", "todo"],
  ["Doing", "doing"],
  ["Test / Review", "review"],
  ["Done", "done"],
]);

const VALID_PRIORITIES = new Set(["critical", "high", "medium", "low"]);

function ensureTrailingNewline(value) {
  return value.endsWith("\n") ? value : `${value}\n`;
}

function normalizeList(value) {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
  .map((entry) => `${entry}`.trim())
  .filter(Boolean);
}

function renderLegacyNotes(parsed) {
  const notes = [];
  const milestone = `${parsed.data.milestone ?? ""}`.trim();
  const dependencies = normalizeList(parsed.data.dependencies);
  const documentation = normalizeList(parsed.data.documentation);
  const modifiedFiles = normalizeList(parsed.data.modified_files);

  if (milestone) {
    notes.push(`Milestone: \`${milestone}\``);
  }

  if (dependencies.length > 0) {
    notes.push(
      `Dependencies: ${dependencies.map((entry) => `\`${entry}\``)
      .join(", ")}`);
  }

  if (documentation.length > 0) {
    notes.push(`Documentation: ${documentation.map((entry) => `\`${entry}\``)
    .join(", ")}`);
  }

  if (modifiedFiles.length > 0) {
    notes.push(
      `Likely affected files: ${modifiedFiles.map((entry) => `\`${entry}\``)
      .join(", ")}`);
  }

  if (notes.length === 0) {
    return "";
  }

  return `## Legacy Notes\n\n${notes.map((note) => `- ${note}`).join("\n")}`;
}

export function buildTicketFilename(prefix, id) {
  return `${prefix ? `${prefix}-` : ""}${id}.md`;
}

export function parseTaskIdNumber(value, fallbackPath = "") {
  const raw = `${value ?? ""}`.trim();
  const fromId = raw.match(/(\d+(?:\.\d+)?)$/)?.[1];

  if (fromId) {
    return Number(fromId);
  }

  const fallbackStem = path.basename(fallbackPath, path.extname(fallbackPath));
  const fromFallback = fallbackStem.match(/(\d+(?:\.\d+)?)/)?.[1];
  if (fromFallback) {
    return Number(fromFallback);
  }

  throw new Error(
    `Could not derive numeric task id from "${raw || fallbackPath}"`);
}

export function mapBacklogStatus(status) {
  return STATUS_MAP.get(`${status ?? ""}`.trim()) ?? "backlog";
}

export function normalizePriority(priority) {
  const normalized = `${priority ?? ""}`.trim().toLowerCase();
  return VALID_PRIORITIES.has(normalized) ? normalized : "medium";
}

export function convertBacklogTask(rawContent, fallbackPath = "") {
  const parsed = matter(rawContent);
  const body = parsed.content.trim();
  const legacyNotes = renderLegacyNotes(parsed);
  const tags = normalizeList(parsed.data.labels);
  const title = `${parsed.data.title ??
  path.basename(fallbackPath, path.extname(fallbackPath))}`.trim();

  return {
    frontmatter: {
      id: parseTaskIdNumber(parsed.data.id, fallbackPath),
      title,
      status: mapBacklogStatus(parsed.data.status),
      priority: normalizePriority(parsed.data.priority),
      tags,
    },
    body: [body, legacyNotes].filter(Boolean).join("\n\n").trim(),
  };
}

export function convertBacklogPage(rawContent, fallbackTitle = "") {
  const parsed = matter(rawContent);
  const title = `${parsed.data.title ?? fallbackTitle}`.trim();

  return {
    frontmatter: {
      title,
    },
    body: parsed.content.trim(),
  };
}

export function buildAssigneeFollowUpTicket(id) {
  return {
    frontmatter: {
      id,
      title: "Add assignee support to the VitePress board",
      status: "backlog",
      priority: "medium",
      tags: ["docs-workflow"],
    },
    body: [
      "## Description",
      "",
      "Add structured assignee support to the VitePress board UI and markdown ticket workflow.",
      "",
      "## Acceptance Criteria",
      "",
      "- [ ] Tasks can store an assignee in frontmatter.",
      "- [ ] The board UI can show and edit the assignee value in local dev mode.",
      "- [ ] The detail view presents the assignee clearly without breaking the stock task flow.",
    ].join("\n"),
  };
}

export function renderMarkdownDocument(frontmatter, body) {
  return ensureTrailingNewline(
    matter.stringify(`\n${body.trim()}\n`, frontmatter));
}
