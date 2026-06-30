import {withSiteBase} from "./site-url.mjs";

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

export function buildPlanPageUrl(relativePath, base = "/") {
  const normalized = normalizePath(relativePath);
  if (!normalized.endsWith(".md")) {
    throw new Error(`Expected markdown path: ${relativePath}`);
  }

  if (normalized === "index.md") {
    return withSiteBase("/", base);
  }

  if (normalized.endsWith("/index.md")) {
    return withSiteBase(`/${normalized.slice(0, -"index.md".length)}`, base);
  }

  return withSiteBase(`/${normalized.replace(/\.md$/i, "")}`, base);
}
