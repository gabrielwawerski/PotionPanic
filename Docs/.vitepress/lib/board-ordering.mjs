function normalizeOrderValue(value) {
  const order = Number(value);
  return Number.isFinite(order) ? order : null;
}

export function compareTicketsByOrder(left, right) {
  const leftOrder = normalizeOrderValue(left?.order);
  const rightOrder = normalizeOrderValue(right?.order);

  if (leftOrder !== null && rightOrder !== null && leftOrder !== rightOrder) {
    return leftOrder - rightOrder;
  }

  if (leftOrder !== null && rightOrder === null) {
    return -1;
  }

  if (leftOrder === null && rightOrder !== null) {
    return 1;
  }

  return Number(left?.id || 0) - Number(right?.id || 0);
}

export function sortTicketsByOrder(tickets) {
  return [...tickets].sort(compareTicketsByOrder);
}

function cloneTicket(ticket) {
  return { ...ticket };
}

function groupTicketsByStatus(tickets) {
  const grouped = new Map();

  for (const ticket of tickets) {
    const key = `${ticket.status ?? ""}`.trim() || "backlog";
    const bucket = grouped.get(key) || [];
    bucket.push(cloneTicket(ticket));
    grouped.set(key, bucket);
  }

  for (const [key, bucket] of grouped.entries()) {
    grouped.set(key, sortTicketsByOrder(bucket));
  }

  return grouped;
}

function renumberColumn(tickets, statusKey) {
  return tickets.map((ticket, index) => ({
    ...ticket,
    order: index + 1,
    status: statusKey,
  }));
}

export function reorderTickets({
  targetColumn,
  targetIndex,
  ticketId,
  tickets,
}) {
  const grouped = groupTicketsByStatus(tickets);
  const dragged = tickets.find((ticket) => ticket.id === ticketId);

  if (!dragged) {
    return { reorderedTickets: sortTicketsByOrder(tickets), updates: [] };
  }

  const sourceColumn = `${dragged.status ?? ""}`.trim() || "backlog";
  const sourceTickets = grouped.get(sourceColumn) || [];
  const targetTickets = grouped.get(targetColumn) || [];
  const sourceIndex = sourceTickets.findIndex((ticket) => ticket.id === ticketId);

  if (sourceIndex < 0) {
    return { reorderedTickets: sortTicketsByOrder(tickets), updates: [] };
  }

  const nextSourceTickets = sourceTickets.filter((ticket) => ticket.id !== ticketId);
  let insertIndex = Math.max(0, Math.min(targetIndex, targetTickets.length));

  if (sourceColumn === targetColumn && insertIndex > sourceIndex) {
    insertIndex -= 1;
  }

  const nextTargetTickets = (
    sourceColumn === targetColumn
      ? [...nextSourceTickets]
      : targetTickets.filter((ticket) => ticket.id !== ticketId)
  );

  nextTargetTickets.splice(insertIndex, 0, {
    ...cloneTicket(dragged),
    status: targetColumn,
  });

  const renumberedSource = sourceColumn === targetColumn
    ? renumberColumn(nextTargetTickets, targetColumn)
    : renumberColumn(nextSourceTickets, sourceColumn);
  const renumberedTarget = sourceColumn === targetColumn
    ? renumberedSource
    : renumberColumn(nextTargetTickets, targetColumn);

  grouped.set(sourceColumn, renumberedSource);
  grouped.set(targetColumn, renumberedTarget);

  const updatedById = new Map();
  for (const bucket of grouped.values()) {
    for (const ticket of bucket) {
      updatedById.set(ticket.id, ticket);
    }
  }

  const updatedTickets = tickets.map((ticket) => (
    updatedById.get(ticket.id) || cloneTicket(ticket)
  ));

  const reorderedTickets = sortTicketsByOrder(updatedTickets);
  const updates = reorderedTickets
    .filter((ticket) => {
      const previous = tickets.find((entry) => entry.id === ticket.id);
      return previous
        && (previous.status !== ticket.status || previous.order !== ticket.order);
    })
    .map((ticket) => ({
      id: ticket.id,
      status: ticket.status,
      order: ticket.order,
      url: ticket.url,
    }));

  return { reorderedTickets, updates };
}
