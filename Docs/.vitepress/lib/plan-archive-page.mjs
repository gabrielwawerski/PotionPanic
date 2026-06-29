function normalizePath(value) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/").replace(/^\/+|\/+$/g, "");
}

export function isArchivablePlanPage(relativePath) {
  const normalized = normalizePath(relativePath);
  if (!normalized.endsWith(".md")) {
    return false;
  }

  return normalized.startsWith("plans/") && normalized !== "plans/index.md";
}

export function buildPlanPageUrl(relativePath) {
  const normalized = normalizePath(relativePath);
  if (!normalized.endsWith(".md")) {
    throw new Error(`Expected markdown path: ${relativePath}`);
  }

  if (normalized === "index.md") {
    return "/";
  }

  if (normalized.endsWith("/index.md")) {
    return `/${normalized.slice(0, -"index.md".length)}`;
  }

  return `/${normalized.replace(/\.md$/i, "")}`;
}
