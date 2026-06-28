export const COMPACT_TICKET_DETAIL_BREAKPOINT = 959;

export function isTicketDetailCompactViewport(viewportWidth) {
  return Number(viewportWidth) <= COMPACT_TICKET_DETAIL_BREAKPOINT;
}

export function shouldCollapseTicketDetailMetadataByDefault(viewportWidth) {
  return isTicketDetailCompactViewport(viewportWidth);
}
