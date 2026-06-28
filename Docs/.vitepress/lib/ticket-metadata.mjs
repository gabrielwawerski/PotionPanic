function normalizeEntry(value) {
  return `${value ?? ""}`.trim();
}

export function normalizeTicketList(value) {
  if (!Array.isArray(value)) {
    return [];
  }

  return value.map(normalizeEntry).filter(Boolean);
}

export function parseListInput(value) {
  return `${value ?? ""}`
  .replace(/\r\n/g, "\n")
  .split("\n")
  .map(normalizeEntry)
  .filter(Boolean);
}

export function formatListInput(value) {
  return normalizeTicketList(value).join("\n");
}

export function normalizeTicketMetadata(source = {}) {
  return {
    affectedFiles: normalizeTicketList(
      source.affectedFiles ?? source.modified_files
    ),
    dependencies: normalizeTicketList(source.dependencies),
    documentation: normalizeTicketList(source.documentation),
    milestone: normalizeEntry(source.milestone),
  };
}
