function normalizePath(value) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/");
}

export function buildTicketHref(reference, ticketsDir = "tickets") {
  const normalized = normalizePath(reference)
  .replace(/^\//, "")
  .replace(/\.md$/i, "")
  .replace(/\.html$/i, "");

  if (!normalized) {
    return null;
  }

  if (normalized.startsWith(`${ticketsDir}/`)) {
    return `/${normalized}.html`;
  }

  if (!normalized.includes("/")) {
    return `/${ticketsDir}/${normalized}.html`;
  }

  return `/${normalized}.html`;
}

export function resolveTicketHref(
  reference,
  {
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
    return ticketHrefs[normalized];
  }

  return buildTicketHref(normalized, ticketsDir);
}

export function buildDocumentationHref(reference) {
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
    return "/README.html";
  }

  if (!normalized.endsWith(".md")) {
    return null;
  }

  if (!normalized.includes("/")) {
    return null;
  }

  return `/${normalized.replace(/\.md$/i, ".html")}`;
}
