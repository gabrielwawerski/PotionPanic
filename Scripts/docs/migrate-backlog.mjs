import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import {
  buildAssigneeFollowUpTicket,
  buildTicketFilename,
  convertBacklogPage,
  convertBacklogTask,
  renderMarkdownDocument,
} from "./lib/backlog-to-vitepress.mjs";

const filePath = fileURLToPath(import.meta.url);
const scriptDirectory = path.dirname(filePath);
const repositoryRoot = path.resolve(scriptDirectory, "..", "..");
const backlogRoot = path.join(repositoryRoot, "backlog");
const docsRoot = path.join(repositoryRoot, "Docs");

if (!fs.existsSync(backlogRoot)) {
  throw new Error(
    "Legacy backlog/ source not found. This migration script is only meant to run before the Backlog tree is retired.");
}

function ensureDirectory(directoryPath) {
  fs.mkdirSync(directoryPath, { recursive: true });
}

function listMarkdownFiles(directoryPath) {
  if (!fs.existsSync(directoryPath)) {
    return [];
  }

  return fs
  .readdirSync(directoryPath)
  .filter((entry) => entry.endsWith(".md"))
  .sort((left, right) => left.localeCompare(right));
}

function writeFile(targetPath, content) {
  ensureDirectory(path.dirname(targetPath));
  fs.writeFileSync(targetPath, content);
}

function makeRelativeLink(fromDirectory, targetPath) {
  return path.relative(fromDirectory, targetPath).replace(/\\/g, "/");
}

function migrateTasks() {
  const sourceDirectory = path.join(backlogRoot, "tasks");
  const targetDirectory = path.join(docsRoot, "tickets");
  const ids = [];

  ensureDirectory(targetDirectory);

  for (const entry of listMarkdownFiles(sourceDirectory)) {
    const absoluteSource = path.join(sourceDirectory, entry);
    const rawContent = fs.readFileSync(absoluteSource, "utf8");
    const converted = convertBacklogTask(rawContent, absoluteSource);
    ids.push(converted.frontmatter.id);

    const targetPath = path.join(targetDirectory,
      buildTicketFilename("PP", converted.frontmatter.id));
    writeFile(targetPath,
      renderMarkdownDocument(converted.frontmatter, converted.body));
  }

  const nextId = Math.floor(ids.length > 0 ? Math.max(...ids) : 0) + 1;
  const followUpTicket = buildAssigneeFollowUpTicket(nextId);
  const followUpPath = path.join(targetDirectory,
    buildTicketFilename("PP", followUpTicket.frontmatter.id));
  writeFile(followUpPath,
    renderMarkdownDocument(followUpTicket.frontmatter, followUpTicket.body));
}

function migrateMilestones() {
  const sourceDirectory = path.join(backlogRoot, "milestones");
  const targetDirectory = path.join(docsRoot, "milestones");
  const links = [];

  ensureDirectory(targetDirectory);

  for (const entry of listMarkdownFiles(sourceDirectory)) {
    const absoluteSource = path.join(sourceDirectory, entry);
    const rawContent = fs.readFileSync(absoluteSource, "utf8");
    const titleFromFile = path.basename(entry, path.extname(entry));
    const converted = convertBacklogPage(rawContent, titleFromFile);
    const slug = titleFromFile.split(" - ")[0];
    const targetPath = path.join(targetDirectory, `${slug}.md`);

    writeFile(targetPath,
      renderMarkdownDocument(converted.frontmatter, converted.body));
    links.push({
      title: converted.frontmatter.title,
      targetPath,
    });
  }

  const indexPath = path.join(targetDirectory, "index.md");
  const indexBody = [
    "# Milestones",
    "",
    ...links.map(
      (entry) => `- [${entry.title}](${makeRelativeLink(targetDirectory,
        entry.targetPath)})`),
  ].join("\n");
  writeFile(indexPath, `${indexBody}\n`);
}

function migrateCompletedArchive() {
  const sourceDirectory = path.join(backlogRoot, "completed");
  const targetDirectory = path.join(docsRoot, "archive", "completed");
  const links = [];

  ensureDirectory(targetDirectory);

  for (const entry of listMarkdownFiles(sourceDirectory)) {
    const absoluteSource = path.join(sourceDirectory, entry);
    const rawContent = fs.readFileSync(absoluteSource, "utf8");
    const converted = convertBacklogPage(rawContent,
      path.basename(entry, path.extname(entry)));
    const slug = path.basename(entry, path.extname(entry)).split(" - ")[0];
    const targetPath = path.join(targetDirectory, `${slug}.md`);

    writeFile(targetPath,
      renderMarkdownDocument(converted.frontmatter, converted.body));
    links.push({
      title: converted.frontmatter.title,
      targetPath,
    });
  }

  const archiveRoot = path.join(docsRoot, "archive");
  const indexPath = path.join(archiveRoot, "index.md");
  const indexBody = [
    "# Archive",
    "",
    "Completed Backlog work migrated into the VitePress documentation site lives here.",
    "",
    "## Completed Tasks",
    "",
    ...links.map((entry) => `- [${entry.title}](${makeRelativeLink(archiveRoot,
      entry.targetPath)})`),
  ].join("\n");
  writeFile(indexPath, `${indexBody}\n`);
}

migrateTasks();
migrateMilestones();
migrateCompletedArchive();
