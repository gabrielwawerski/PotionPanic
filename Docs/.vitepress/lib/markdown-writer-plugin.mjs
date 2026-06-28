import fs from "node:fs";
import path from "node:path";

import matter from "gray-matter";

import {
  buildTicketTemplate,
  ensureTicketSections,
  findMissingTicketSections,
  normalizeTicketSections,
} from "./ticket-sections.mjs";
import {
  normalizeTicketList,
  normalizeTicketMetadata
} from "./ticket-metadata.mjs";
import {
  buildTicketSuggestionCatalog,
  findAffectedFileSuggestionPaths,
  findDocumentationSuggestionPaths,
  normalizeBoardSuggestionConfig,
} from "./ticket-suggestions.mjs";

export function scanTickets(ticketsDir, dirRelative) {
  if (!fs.existsSync(ticketsDir)) {
    return [];
  }

  const files = fs.readdirSync(ticketsDir)
  .filter((file) => file.endsWith(".md"));

  return files.map((file) => {
    const raw = fs.readFileSync(path.join(ticketsDir, file), "utf8");
    const parsed = matter(raw);
    const metadata = normalizeTicketMetadata(parsed.data);

    return {
      affectedFiles: metadata.affectedFiles,
      assignee: `${parsed.data.assignee ?? ""}`.trim(),
      dependencies: metadata.dependencies,
      documentation: metadata.documentation,
      id: Number(parsed.data.id) || 0,
      milestone: metadata.milestone,
      title: parsed.data.title || path.basename(file, ".md"),
      status: parsed.data.status || "backlog",
      priority: parsed.data.priority || "medium",
      tags: parsed.data.tags || [],
      body: parsed.content.trim(),
      url: `/${dirRelative}/${path.basename(file, ".md")}.html`,
    };
  });
}

function listTicketEntries(ticketsDir) {
  if (!fs.existsSync(ticketsDir)) {
    return [];
  }

  const files = fs.readdirSync(ticketsDir)
  .filter((file) => file.endsWith(".md"));
  const entries = files.map((file) => {
    const raw = fs.readFileSync(path.join(ticketsDir, file), "utf8");
    const parsed = matter(raw);
    const id = Number(parsed.data.id) || 0;
    const slug = path.basename(file, ".md");

    return {
      file,
      id,
      parsed,
      raw,
      slug,
    };
  });

  const idCounts = new Map();
  for (const entry of entries) {
    if (entry.id > 0) {
      idCounts.set(entry.id, (idCounts.get(entry.id) || 0) + 1);
    }
  }

  return { entries, idCounts };
}

export function validateTickets(
  ticketsDir,
  dirRelative,
  prefix,
  configuredSections = []
) {
  if (!fs.existsSync(ticketsDir)) {
    return [];
  }

  const { entries, idCounts } = listTicketEntries(ticketsDir);
  const normalizedSections = normalizeTicketSections(configuredSections);
  const issues = [];
  const goodIds = new Set();

  for (const entry of entries) {
    const expectedSlug = prefix ? `${prefix}-${entry.id}` : String(entry.id);
    const isDuplicate = entry.id > 0 && (idCounts.get(entry.id) || 0) > 1;
    const isMissing = entry.id <= 0;
    const isMismatch = entry.id > 0 && entry.slug !== expectedSlug;

    if (!isDuplicate && !isMissing) {
      goodIds.add(entry.id);
    }
  }

  let nextFixId = 1;

  for (const entry of entries) {
    const expectedSlug = prefix ? `${prefix}-${entry.id}` : String(entry.id);
    const isDuplicate = entry.id > 0 && (idCounts.get(entry.id) || 0) > 1;
    const isMissing = entry.id <= 0;
    const isMismatch = entry.id > 0 && entry.slug !== expectedSlug;

    if (isDuplicate || isMissing || isMismatch) {
      let fixedId = entry.id;

      if (isDuplicate || isMissing) {
        while (goodIds.has(nextFixId)) {
          nextFixId += 1;
        }
        fixedId = nextFixId;
        goodIds.add(nextFixId);
        nextFixId += 1;
      }

      issues.push({
        type: "identity",
        file: entry.file,
        currentId: entry.id,
        currentSlug: entry.slug,
        fixedId,
        fixedSlug: prefix ? `${prefix}-${fixedId}` : String(fixedId),
      });
    }

    const missingSections = findMissingTicketSections(
      entry.parsed.content,
      normalizedSections
    );

    if (missingSections.length > 0) {
      issues.push({
        type: "missing-sections",
        file: entry.file,
        currentId: entry.id,
        currentSlug: entry.slug,
        missingSections,
      });
    }
  }

  return issues;
}

export function fixTickets(
  ticketsDir,
  dirRelative,
  prefix,
  configuredSections = []
) {
  const issues = validateTickets(
    ticketsDir,
    dirRelative,
    prefix,
    configuredSections
  );

  if (issues.length === 0) {
    return [];
  }

  const issuesByFile = new Map();
  for (const issue of issues) {
    const grouped = issuesByFile.get(issue.file) || [];
    grouped.push(issue);
    issuesByFile.set(issue.file, grouped);
  }

  for (const [file, fileIssues] of issuesByFile.entries()) {
    const oldPath = path.join(ticketsDir, file);
    const raw = fs.readFileSync(oldPath, "utf8");
    const parsed = matter(raw);
    const identityIssue = fileIssues.find((issue) => issue.type === "identity");
    const sectionIssue = fileIssues.find(
      (issue) => issue.type === "missing-sections");

    if (identityIssue) {
      parsed.data.id = identityIssue.fixedId;
    }

    if (sectionIssue) {
      parsed.content = `\n${ensureTicketSections(
        parsed.content,
        configuredSections
      )}`;
    }

    const output = matter.stringify(parsed.content, parsed.data);
    const targetSlug = identityIssue?.fixedSlug ?? path.basename(file, ".md");
    const newPath = path.join(ticketsDir, `${targetSlug}.md`);
    fs.writeFileSync(newPath, output);

    if (newPath !== oldPath && fs.existsSync(oldPath)) {
      fs.unlinkSync(oldPath);
    }
  }

  return issues;
}

export function getMaxTicketId(ticketsDir) {
  if (!fs.existsSync(ticketsDir)) {
    return 0;
  }

  const files = fs.readdirSync(ticketsDir)
  .filter((file) => file.endsWith(".md"));
  let max = 0;

  for (const file of files) {
    const raw = fs.readFileSync(path.join(ticketsDir, file), "utf8");
    const parsed = matter(raw);
    const id = Number(parsed.data.id);

    if (id > max) {
      max = id;
    }
  }

  return max;
}

export function createTicketFile(
  ticketsDir,
  {
    affectedFiles = [],
    assignee = "",
    body = "",
    dependencies = [],
    documentation = [],
    dirRelative = "tickets",
    milestone = "",
    prefix = "",
    priority = "medium",
    sections = [],
    status = "backlog",
    tags = [],
    title = "New ticket",
  } = {}
) {
  if (!fs.existsSync(ticketsDir)) {
    fs.mkdirSync(ticketsDir, { recursive: true });
  }

  const id = getMaxTicketId(ticketsDir) + 1;
  const slug = prefix ? `${prefix}-${id}` : String(id);
  const contentBody = `${body ?? ""}`.trim() || buildTicketTemplate(sections);
  const frontmatter = { id, title, status, priority };
  const normalizedAssignee = `${assignee ?? ""}`.trim();
  const metadata = normalizeTicketMetadata({
    affectedFiles,
    dependencies,
    documentation,
    milestone,
  });

  if (Array.isArray(tags) && tags.length > 0) {
    frontmatter.tags = tags;
  }
  if (normalizedAssignee) {
    frontmatter.assignee = normalizedAssignee;
  }
  if (metadata.milestone) {
    frontmatter.milestone = metadata.milestone;
  }
  if (metadata.dependencies.length > 0) {
    frontmatter.dependencies = metadata.dependencies;
  }
  if (metadata.documentation.length > 0) {
    frontmatter.documentation = metadata.documentation;
  }
  if (metadata.affectedFiles.length > 0) {
    frontmatter.affectedFiles = metadata.affectedFiles;
  }

  const content = matter.stringify(`\n${contentBody}\n`, frontmatter);
  const filePath = path.join(ticketsDir, `${slug}.md`);
  fs.writeFileSync(filePath, content);

  return {
    affectedFiles: metadata.affectedFiles,
    assignee: normalizedAssignee,
    dependencies: metadata.dependencies,
    documentation: metadata.documentation,
    id,
    milestone: metadata.milestone,
    title,
    status,
    priority,
    tags: Array.isArray(tags) ? tags : [],
    body: contentBody,
    url: `/${dirRelative}/${slug}.html`,
  };
}

function findMarkdownFiles(dir) {
  const results = [];

  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
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

function findBoardConfig(srcDir, dirRelative) {
  for (const file of findMarkdownFiles(srcDir)) {
    const raw = fs.readFileSync(file, "utf8");
    const parsed = matter(raw);

    if (!parsed.data.board) {
      continue;
    }

    if ((parsed.data.ticketsDir || "tickets") === dirRelative) {
      return parsed.data;
    }
  }

  return {};
}

function createSuggestionCatalog(srcDir, dirRelative, prefix = "") {
  const ticketsDir = path.resolve(srcDir, dirRelative);
  const boardConfig = findBoardConfig(srcDir, dirRelative);
  const ticketPrefix = prefix || boardConfig.ticketPrefix || "";
  const repoRoot = path.resolve(srcDir, "..");
  const ticketRepoPath = path.relative(repoRoot, ticketsDir);

  return buildTicketSuggestionCatalog({
    affectedFilePaths: findAffectedFileSuggestionPaths(repoRoot, ticketRepoPath),
    boardSuggestions: normalizeBoardSuggestionConfig(
      boardConfig.ticketFieldSuggestions
    ),
    documentationPaths: findDocumentationSuggestionPaths(srcDir, dirRelative),
    prefix: ticketPrefix,
    tickets: scanTickets(ticketsDir, dirRelative),
  });
}

export function markdownWriterPlugin() {
  let srcDir = "";

  return {
    name: "vitepress-pm-markdown-writer",
    configResolved(config) {
      srcDir = config.root;
    },
    configureServer(server) {
      server.middlewares.use("/__vitepress_pm_tickets", (req, res) => {
        const url = new URL(req.url || "/", "http://localhost");
        const dir = url.searchParams.get("dir") || "tickets";
        const ticketsDir = path.resolve(srcDir, dir);
        const tickets = scanTickets(ticketsDir, dir);

        res.setHeader("Content-Type", "application/json");
        res.end(JSON.stringify(tickets));
      });

      server.middlewares.use("/__vitepress_pm_validate", (req, res) => {
        const url = new URL(req.url || "/", "http://localhost");
        const dir = url.searchParams.get("dir") || "tickets";
        const prefix = url.searchParams.get("prefix") || "";
        const sections = url.searchParams.getAll("section");
        const ticketsDir = path.resolve(srcDir, dir);
        const issues = validateTickets(ticketsDir, dir, prefix, sections);

        res.setHeader("Content-Type", "application/json");
        res.end(JSON.stringify(issues));
      });

      server.middlewares.use("/__vitepress_pm_suggestions", (req, res) => {
        const url = new URL(req.url || "/", "http://localhost");
        const dir = url.searchParams.get("dir") || "tickets";
        const prefix = url.searchParams.get("prefix") || "";
        const suggestions = createSuggestionCatalog(srcDir, dir, prefix);

        res.setHeader("Content-Type", "application/json");
        res.end(JSON.stringify(suggestions));
      });

      server.middlewares.use("/__vitepress_pm_fix", (req, res) => {
        if (req.method !== "POST") {
          res.statusCode = 405;
          res.end("Method not allowed");
          return;
        }

        let body = "";
        req.on("data", (chunk) => {
          body += chunk;
        });
        req.on("end", () => {
          try {
            const { dir, prefix, sections } = JSON.parse(body);
            const ticketsDir = path.resolve(srcDir, dir || "tickets");
            const fixed = fixTickets(
              ticketsDir,
              dir || "tickets",
              prefix || "",
              sections || []
            );

            res.setHeader("Content-Type", "application/json");
            res.end(JSON.stringify(fixed));
          } catch (cause) {
            res.statusCode = 500;
            res.end(String(cause));
          }
        });
      });

      server.middlewares.use("/__vitepress_pm_create", (req, res) => {
        if (req.method !== "POST") {
          res.statusCode = 405;
          res.end("Method not allowed");
          return;
        }

        let body = "";
        req.on("data", (chunk) => {
          body += chunk;
        });
        req.on("end", () => {
          try {
            const {
              body: ticketBody,
              dir,
              prefix,
              priority,
              affectedFiles,
              assignee,
              dependencies,
              documentation,
              milestone,
              sections,
              status,
              tags,
              title,
            } = JSON.parse(body);
            const ticketsDir = path.resolve(srcDir, dir || "tickets");
            const ticket = createTicketFile(ticketsDir, {
              body: ticketBody,
              dirRelative: dir || "tickets",
              prefix: prefix || "",
              priority,
              affectedFiles,
              assignee,
              dependencies,
              documentation,
              milestone,
              sections,
              status,
              tags,
              title,
            });

            res.setHeader("Content-Type", "application/json");
            res.end(JSON.stringify(ticket));
          } catch (cause) {
            res.statusCode = 500;
            res.end(String(cause));
          }
        });
      });

      server.middlewares.use("/__vitepress_pm_update", (req, res) => {
        if (req.method !== "POST") {
          res.statusCode = 405;
          res.end("Method not allowed");
          return;
        }

        let body = "";
        req.on("data", (chunk) => {
          body += chunk;
        });
        req.on("end", () => {
          try {
            const { url, updates } = JSON.parse(body);

            if (!url || typeof url !== "string") {
              res.statusCode = 400;
              res.end("Missing url");
              return;
            }

            const mdPath = url.replace(/\.html$/, ".md").replace(/^\//, "");
            const filePath = path.resolve(srcDir, mdPath);

            if (!fs.existsSync(filePath)) {
              res.statusCode = 404;
              res.end(`File not found: ${mdPath}`);
              return;
            }

            const raw = fs.readFileSync(filePath, "utf8");
            const parsed = matter(raw);

            for (const [key, value] of Object.entries(updates || {})) {
              if (key === "body") {
                parsed.content = `\n${String(value)}\n`;
              }
              else if (key === "assignee") {
                const normalizedAssignee = `${value ?? ""}`.trim();
                if (normalizedAssignee) {
                  parsed.data.assignee = normalizedAssignee;
                }
                else {
                  delete parsed.data.assignee;
                }
              }
              else if (key === "milestone") {
                const normalizedMilestone = `${value ?? ""}`.trim();
                if (normalizedMilestone) {
                  parsed.data.milestone = normalizedMilestone;
                }
                else {
                  delete parsed.data.milestone;
                }
              }
              else if (
                key === "dependencies"
                || key === "documentation"
                || key === "affectedFiles"
              ) {
                const normalizedList = normalizeTicketList(value);
                if (normalizedList.length > 0) {
                  parsed.data[key] = normalizedList;
                }
                else {
                  delete parsed.data[key];
                }
              }
              else {
                parsed.data[key] = value;
              }
            }

            const output = matter.stringify(parsed.content, parsed.data);
            fs.writeFileSync(filePath, output);
            res.statusCode = 200;
            res.end("ok");
          } catch (cause) {
            res.statusCode = 500;
            res.end(String(cause));
          }
        });
      });
    },
    generateBundle() {
      const markdownFiles = findMarkdownFiles(srcDir);
      const seenDirs = new Set();

      for (const file of markdownFiles) {
        const raw = fs.readFileSync(file, "utf8");
        const parsed = matter(raw);

        if (!parsed.data.board) {
          continue;
        }

        const dir = parsed.data.ticketsDir || "tickets";
        if (seenDirs.has(dir)) {
          continue;
        }

        seenDirs.add(dir);
        const ticketsDir = path.resolve(srcDir, dir);
        const tickets = scanTickets(ticketsDir, dir);
        const suggestions = createSuggestionCatalog(
          srcDir,
          dir,
          parsed.data.ticketPrefix || ""
        );

        this.emitFile({
          type: "asset",
          fileName: `__vitepress_pm_tickets/${dir}.json`,
          source: JSON.stringify(tickets),
        });

        this.emitFile({
          type: "asset",
          fileName: `__vitepress_pm_suggestions/${dir}.json`,
          source: JSON.stringify(suggestions),
        });
      }
    },
  };
}
