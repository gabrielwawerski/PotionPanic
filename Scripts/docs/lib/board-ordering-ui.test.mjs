import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const repoRoot = path.resolve(import.meta.dirname, "../../..");
const boardPath = path.join(
  repoRoot,
  "Docs/.vitepress/theme/components/Board.vue"
);
const boardColumnPath = path.join(
  repoRoot,
  "Docs/.vitepress/theme/components/BoardColumn.vue"
);
const ticketWriterPath = path.join(
  repoRoot,
  "Docs/.vitepress/theme/composables/useTicketWriter.ts"
);

test("board column exposes insertion targets instead of column-only drop",
  () => {
    const source = fs.readFileSync(boardColumnPath, "utf8");

    assert.match(
      source,
      /drop: \[event: DragEvent, index: number\]/,
      "column drop events should include the insertion index"
    );
    assert.match(
      source,
      /v-for="\(ticket, index\) in tickets"/,
      "column template should track ticket indexes for insertion targets"
    );
  });

test("ticket writer exposes a batch update path for multi-ticket reorders",
  () => {
    const source = fs.readFileSync(ticketWriterPath, "utf8");

    assert.match(
      source,
      /async function writeTickets\(updates: Array<\{url: string; updates: Record<string, unknown>\}>\)/,
      "writer should expose a batch ticket update helper"
    );
    assert.match(
      source,
      /postJson\(\"\/__vitepress_pm_update_batch\", \{updates\}\)/,
      "writer should post batched updates to the batch endpoint"
    );
  });

test("board uses the batch ticket writer when drag ordering updates multiple cards",
  () => {
    const source = fs.readFileSync(boardPath, "utf8");

    assert.match(
      source,
      /const \{ archiveTicket, restoreTicket, writeTicket, writeTickets \} = useTicketWriter\(\)/,
      "board should request the batch writer from the composable"
    );
    assert.match(
      source,
      /writeTickets\(reorderUpdates\)/,
      "board should persist reorder changes through the batch writer"
    );
  });
