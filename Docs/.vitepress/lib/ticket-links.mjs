import {withSiteBase} from "./site-url.mjs";

function normalizePath(value) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/");
}

export function buildTicketHref(reference, ticketsDir = "tickets", base = "/") {
  const normalized = normalizePath(reference)
  .replace(/^\//, "")
  .replace(/\.md$/i, "")
  .replace(/\.html$/i, "");

  if (!normalized) {
    return null;
  }

  if (normalized.startsWith(`${ticketsDir}/`)) {
    return withSiteBase(`/${normalized}.html`, base);
  }

  if (!normalized.includes("/")) {
    return withSiteBase(`/${ticketsDir}/${normalized}.html`, base);
  }

  return withSiteBase(`/${normalized}.html`, base);
}

export function resolveTicketHref(
  reference,
  {
    base = "/",
    ticketHrefs = {},
    ticketsDir = "tickets",
  } = {}
) {
  const normalized = normalizePath(reference)
  .replace(/^\//, "")
  .replace(/\.md$/i, "")
  .replace(/\.html$/i, "");

  if (!normalized) {
    return null;
  }

  if (ticketHrefs[normalized]) {
    return withSiteBase(ticketHrefs[normalized], base);
  }

  return buildTicketHref(normalized, ticketsDir, base);
}

export function buildDocumentationHref(reference, base = "/") {
  let normalized = normalizePath(reference);
  if (!normalized) {
    return null;
  }

  normalized = normalized.replace(/^\.\/+/, "");
  while (normalized.startsWith("../")) {
    normalized = normalized.slice(3);
  }
  if (normalized.startsWith("Docs/")) {
    normalized = normalized.slice(5);
  }

  if (normalized === "README.md") {
    return withSiteBase("/README.html", base);
  }

  if (!normalized.endsWith(".md")) {
    return null;
  }

  if (!normalized.includes("/")) {
    return null;
  }

  return withSiteBase(`/${normalized.replace(/\.md$/i, ".html")}`, base);
}
