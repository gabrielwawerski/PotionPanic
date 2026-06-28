export const DEFAULT_TICKET_SECTIONS = [
  "Description",
  "Acceptance Criteria",
  "Implementation Plan",
  "Implementation Notes",
  "Definition of Done",
  "Notes",
];

function trimBlankLines(value) {
  return `${value ?? ""}`.replace(/\r\n/g, "\n").replace(/^\n+|\n+$/g, "");
}

export function normalizeTicketSections(sections) {
  const normalized = Array.isArray(sections)
    ? sections
    .map((entry) => `${entry ?? ""}`.trim())
    .filter(Boolean)
    : [];

  return [
    ...new Set(normalized.length > 0 ? normalized : DEFAULT_TICKET_SECTIONS)
  ];
}

export function parseTicketSections(markdown,
  configuredSections = DEFAULT_TICKET_SECTIONS) {
  const headings = normalizeTicketSections(configuredSections);
  const source = `${markdown ?? ""}`.replace(/\r\n/g, "\n");
  const lines = source.split("\n");
  const parsedSections = [];
  const preamble = [];
  let currentHeading = null;
  let currentLines = [];

  function flushCurrent() {
    if (!currentHeading) {
      return;
    }

    parsedSections.push({
      heading: currentHeading,
      content: trimBlankLines(currentLines.join("\n")),
    });
  }

  for (const line of lines) {
    const match = line.match(/^## (.+)$/);

    if (match) {
      flushCurrent();
      currentHeading = match[1].trim();
      currentLines = [];
      continue;
    }

    if (!currentHeading) {
      preamble.push(line);
      continue;
    }

    currentLines.push(line);
  }

  flushCurrent();

  const parsedByHeading = new Map(
    parsedSections.map((section) => [section.heading, section.content])
  );
  const orderedSections = [];
  const seen = new Set();
  const intro = trimBlankLines(preamble.join("\n"));

  if (intro) {
    const firstHeading = headings[0];
    const existing = parsedByHeading.get(firstHeading);
    parsedByHeading.set(
      firstHeading,
      trimBlankLines([intro, existing].filter(Boolean).join("\n\n"))
    );
  }

  for (const heading of headings) {
    const content = parsedByHeading.get(heading);
    orderedSections.push({
      heading,
      content: content ?? "",
      missing: content == null,
    });
    seen.add(heading);
  }

  for (const section of parsedSections) {
    if (seen.has(section.heading)) {
      continue;
    }

    orderedSections.push({
      heading: section.heading,
      content: section.content,
      extra: true,
      missing: false,
    });
  }

  return orderedSections;
}

export function serializeTicketSections(sections) {
  const normalizedSections = Array.isArray(sections) ? sections : [];
  const blocks = normalizedSections.map((section) => {
    const heading = `${section?.heading ?? ""}`.trim();
    if (!heading) {
      return "";
    }

    const content = trimBlankLines(section?.content ?? "");
    return content ? `## ${heading}\n\n${content}` : `## ${heading}`;
  }).filter(Boolean);

  if (blocks.length === 0) {
    return "";
  }

  return `${blocks.join("\n\n")}\n`;
}

export function buildTicketTemplate(configuredSections = DEFAULT_TICKET_SECTIONS) {
  return serializeTicketSections(
    normalizeTicketSections(configuredSections).map((heading) => ({
      heading,
      content: "",
    }))
  );
}

export function findMissingTicketSections(markdown,
  configuredSections = DEFAULT_TICKET_SECTIONS) {
  return parseTicketSections(markdown, configuredSections)
  .filter((section) => section.missing)
  .map((section) => section.heading);
}

export function ensureTicketSections(markdown,
  configuredSections = DEFAULT_TICKET_SECTIONS) {
  return serializeTicketSections(
    parseTicketSections(markdown, configuredSections));
}
