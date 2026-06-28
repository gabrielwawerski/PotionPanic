#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

import matter from "gray-matter";

import {
  createTicketFile
} from "../../Docs/.vitepress/lib/markdown-writer-plugin.mjs";

function printUsage(exitCode = 0) {
  console.log(`Usage:
  npm run docs:ticket -- "Ticket title"
  npm run docs:ticket -- "Ticket title" --status doing --priority high --tags docs,workflow

Options:
  --dir <path>       Tickets directory (default: Docs/tickets)
  --status <status>  Initial status (default: backlog)
  --priority <pri>   Priority: critical, high, medium, low (default: medium)
  --tags <tags>      Comma-separated tags
  --body <text>      Ticket body markdown
  --prefix <prefix>  Ticket ID prefix (overrides board.md ticketPrefix)`);
  process.exit(exitCode);
}

function parseArgs(args) {
  let title = "";
  let dir = "Docs/tickets";
  let status = "backlog";
  let priority = "medium";
  let tags = [];
  let body = "";
  let prefix = null;

  let index = 0;
  while (index < args.length) {
    if (args[index] === "--dir") {
      dir = args[index + 1];
      index += 2;
      continue;
    }
    if (args[index] === "--status") {
      status = args[index + 1];
      index += 2;
      continue;
    }
    if (args[index] === "--priority") {
      priority = args[index + 1];
      index += 2;
      continue;
    }
    if (args[index] === "--tags") {
      tags = args[index + 1]
      .split(",")
      .map((entry) => entry.trim())
      .filter(Boolean);
      index += 2;
      continue;
    }
    if (args[index] === "--body") {
      body = args[index + 1];
      index += 2;
      continue;
    }
    if (args[index] === "--prefix") {
      prefix = args[index + 1];
      index += 2;
      continue;
    }
    if (!title) {
      title = args[index];
    }
    index += 1;
  }

  return { body, dir, prefix, priority, status, tags, title };
}

function readBoardSettings(siteDir) {
  const boardPath = path.join(siteDir, "board.md");

  if (!fs.existsSync(boardPath)) {
    return { ticketPrefix: "", ticketSections: [] };
  }

  const raw = fs.readFileSync(boardPath, "utf8");
  const parsed = matter(raw);

  return {
    ticketPrefix: parsed.data.ticketPrefix || "",
    ticketSections: Array.isArray(parsed.data.ticketSections)
      ? parsed.data.ticketSections
      : [],
  };
}

const args = process.argv.slice(2);
if (args.length === 0 || args[0] === "--help" || args[0] === "-h") {
  printUsage(args.length === 0 ? 1 : 0);
}

const options = parseArgs(args);

if (!options.title) {
  console.error("Error: title is required.");
  printUsage(1);
}

const ticketsDir = path.resolve(process.cwd(), options.dir);
const siteDir = path.resolve(ticketsDir, "..");
const boardSettings = readBoardSettings(siteDir);
const ticket = createTicketFile(ticketsDir, {
  body: options.body,
  dirRelative: path.relative(siteDir, ticketsDir).replace(/\\/g, "/"),
  prefix: options.prefix ?? boardSettings.ticketPrefix,
  priority: options.priority,
  sections: boardSettings.ticketSections,
  status: options.status,
  tags: options.tags,
  title: options.title,
});
const displayId = (options.prefix ?? boardSettings.ticketPrefix)
  ? `${options.prefix ?? boardSettings.ticketPrefix}-${ticket.id}`
  : String(ticket.id);
const filePath = path.relative(process.cwd(),
  ticket.url.replace(/^\//, "").replace(/\.html$/, ".md"));

console.log(`Created ${displayId}: ${ticket.title}`);
console.log(`  File: ${filePath}`);
