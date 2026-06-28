# VitePress Project Management Feature Proposals

This repo currently uses a locally owned VitePress kanban UI backed by markdown
tickets in `Docs/tickets/` and archived task history in
`Docs/archive/completed/`. The current ticket modal exposes the whole markdown
body as a single `Description` editor even though many tickets already use a
richer multi-section structure.

## Current Pain Point

The ticket modal only treats the body as one block. That makes active tickets
feel flatter than archived tickets, even when the markdown already contains
sections such as `Acceptance Criteria`, `Implementation Plan`,
`Definition of Done`, and `Notes`.

## Feature Proposals

1. **Section-aware ticket modal**
   Parse `##` headings and render them as first-class sections in the modal
   instead of one large `Description` block.
2. **Configurable section schema**
   Let a board define the expected section list so different projects can use
   the same plugin with different ticket structures.
3. **Template-based new tickets**
   Create new tickets from a reusable section template instead of a blank body.
4. **Structured editor plus raw markdown fallback**
   Support section-by-section editing while still allowing direct markdown edits
   for power users.
5. **Required-section validation**
   Extend ticket validation so the board can warn when required sections are
   missing from a ticket body.
6. **Section-level progress indicators**
   Track checkbox progress per section, not only across the full markdown body.
7. **Structured workflow metadata**
   Promote values such as milestone, dependencies, and documentation links into
   clearer first-class metadata where that improves the board UI.
8. **Dependency links between tickets**
   Turn ticket references such as `PP-2` into clickable relationships in the
   detail modal.
9. **Archive-focused presentation mode**
   Give completed tickets a stronger read-only presentation that emphasizes
   outcome, verification, and history.
10. **Deep links to modal sections**
    Support direct links to a ticket's `Implementation Plan`, `Notes`, or other
    sections from the surrounding docs.

## Recommended First Slice

Start with the smallest set that solves the biggest usability gap:

1. Add a section-aware modal that treats markdown headings as structured ticket
   sections.
2. Add template-based ticket creation so new tickets start with the same
   multi-section shape.
3. Add required-section validation so the board can detect tickets that drift
   away from the expected structure.

This first slice stays close to the existing markdown-first workflow, improves
active ticket editing immediately, and creates a clean foundation for future
features such as assignees, dependency links, and richer archive views.

## Recommended Next Slice

The next best slice is **structured workflow metadata and ticket relationships**.

This repo's active tickets already carry useful planning data such as
milestone, dependencies, documentation links, and likely affected files, but
those values currently live inside markdown content rather than first-class
ticket metadata.

### Scope

1. **Structured workflow metadata**
   Promote `milestone`, `dependencies`, `documentation`, and likely affected
   files into frontmatter-backed fields that the board understands directly.
2. **Sidebar editing and display**
   Show and edit those fields in the ticket modal sidebar alongside status,
   priority, tags, and assignee.
3. **Clickable relationships**
   Render dependency ticket IDs such as `PP-2` and `PP-3` as direct links in
   the modal so tickets become easier to navigate.

### Why This Next

- It improves the day-to-day planning workflow without changing the core board
  model again.
- It builds directly on the new section-aware modal and assignee support.
- It turns ticket data that already exists in this repo into filterable,
  scannable, UI-level information.
- It creates a clean base for later features such as archive-specific views,
  dependency visualizations, and richer validation.

### Recommended Boundaries

Keep this slice intentionally small:

- `milestone` should stay a single string field.
- `dependencies`, `documentation`, and likely affected files should be simple
  string arrays.
- Freeform section notes should still exist for anything that does not fit the
  structured fields cleanly.
- Do not add graph views, calendars, or multi-level planning screens in this
  slice.
