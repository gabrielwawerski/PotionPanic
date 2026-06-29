import assert from "node:assert/strict";
import test from "node:test";

import {
  reorderTickets,
  sortTicketsByOrder,
} from "../../../Docs/.vitepress/lib/board-ordering.mjs";

function makeTicket(id, status, order) {
  return {
    affectedFiles: [],
    assignee: "",
    dependencies: [],
    documentation: [],
    id,
    milestone: "",
    order,
    priority: "medium",
    status,
    tags: [],
    title: `Ticket ${id}`,
    body: "",
    url: `/tickets/PP-${id}.html`,
  };
}

test("sortTicketsByOrder falls back to id order for legacy tickets", () => {
  const sorted = sortTicketsByOrder([
    makeTicket(7, "todo", undefined),
    makeTicket(2, "todo", undefined),
    makeTicket(5, "todo", 1),
  ]);

  assert.deepEqual(
    sorted.map((ticket) => ticket.id),
    [5, 2, 7]
  );
});

test("reorderTickets renumbers a column after an in-column move", () => {
  const {reorderedTickets, updates} = reorderTickets({
    targetColumn: "todo",
    targetIndex: 0,
    ticketId: 3,
    tickets: [
      makeTicket(1, "todo", 1),
      makeTicket(2, "todo", 2),
      makeTicket(3, "todo", 3),
      makeTicket(4, "doing", 1),
    ],
  });

  const todoTickets = reorderedTickets.filter((ticket) => ticket.status === "todo");

  assert.deepEqual(
    todoTickets.map((ticket) => [ticket.id, ticket.order]),
    [[3, 1], [1, 2], [2, 3]]
  );
  assert.deepEqual(
    updates.map((ticket) => [ticket.id, ticket.status, ticket.order]),
    [[3, "todo", 1], [1, "todo", 2], [2, "todo", 3]]
  );
});

test("reorderTickets inserts into another column and renumbers both columns", () => {
  const {reorderedTickets, updates} = reorderTickets({
    targetColumn: "review",
    targetIndex: 1,
    ticketId: 2,
    tickets: [
      makeTicket(1, "todo", 1),
      makeTicket(2, "todo", 2),
      makeTicket(3, "review", 1),
      makeTicket(4, "review", 2),
    ],
  });

  const todoTickets = reorderedTickets.filter((ticket) => ticket.status === "todo");
  const reviewTickets = reorderedTickets.filter((ticket) => ticket.status === "review");

  assert.deepEqual(
    todoTickets.map((ticket) => [ticket.id, ticket.order]),
    [[1, 1]]
  );
  assert.deepEqual(
    reviewTickets.map((ticket) => [ticket.id, ticket.order]),
    [[3, 1], [2, 2], [4, 3]]
  );
  assert.deepEqual(
    updates.map((ticket) => [ticket.id, ticket.status, ticket.order]),
    [[2, "review", 2], [4, "review", 3]]
  );
});
