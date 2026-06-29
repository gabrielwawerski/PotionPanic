export function buildProjectRootPagePath(relativePath) {
  const normalizedRelativePath = `${relativePath ?? ""}`
    .trim()
    .replaceAll("\\", "/")
    .replace(/^\/+/, "");

  if (!normalizedRelativePath) {
    return null;
  }

  return `Docs/${normalizedRelativePath}`;
}
