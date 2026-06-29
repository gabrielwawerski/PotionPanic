<script setup lang="ts">
import {computed, ref} from "vue";
import {useData} from "vitepress";

import {
  buildTicketTemplate,
  normalizeTicketSections
} from "../../lib/ticket-sections.mjs";
import {
  compareTicketsByOrder,
  reorderTickets,
} from "../../lib/board-ordering.mjs";
import {READ_ONLY_BOARD_NOTICE} from "../../lib/board-notice.mjs";

import type {
  Column,
  Ticket,
  TicketSuggestionCatalog,
  TicketValidationIssue
} from "../types";
import {useDragDrop} from "../composables/useDragDrop";
import {useTicketWriter} from "../composables/useTicketWriter";

import BoardColumn from "./BoardColumn.vue";
import TagFilterDropdown from "./TagFilterDropdown.vue";
import TicketDetail from "./TicketDetail.vue";
import TicketFixModal from "./TicketFixModal.vue";

const { frontmatter } = useData()
const { archiveTicket, restoreTicket, writeTicket, writeTickets } = useTicketWriter()

const columns = computed<Column[]>(() => frontmatter.value.columns || [])
const boardMode = computed(() => frontmatter.value.boardMode || "active")
const defaultColumn = computed(() => (
  frontmatter.value.defaultColumn || (columns.value.length > 0 ? columns.value[0].key : 'backlog')
))
const demo = computed(() => !!frontmatter.value.demo)
const archiveTicketsDir = computed(() => (
  frontmatter.value.archiveTicketsDir || "archive/tickets"
))
const canEditBoard = computed(() => import.meta.env.DEV && boardMode.value === "active")
const canRestoreTickets = computed(() => (
  import.meta.env.DEV && boardMode.value === "archive"
))
const ticketPrefix = computed(() => frontmatter.value.ticketPrefix || '')
const restoreTicketsDir = computed(() => (
  frontmatter.value.restoreTicketsDir || "tickets"
))
const ticketsDir = computed(() => frontmatter.value.ticketsDir || 'tickets')
const ticketSections = computed<string[]>(() => (
    normalizeTicketSections(frontmatter.value.ticketSections || [])
));
const readOnly = computed(() => !import.meta.env.DEV)
const ticketReadOnly = computed(() => !import.meta.env.DEV || boardMode.value === "archive")
const hasActiveFilters = computed(() => (
  filter.value.trim().length > 0 || selectedTags.value.length > 0
))
const reorderReadOnly = computed(() => (
  ticketReadOnly.value || hasActiveFilters.value
))

const draftTicket = ref<Ticket | null>(null)
const filter = ref('')
const selectedId = ref<number | null>(null)
const selectedTags = ref<string[]>([])
const showFixModal = ref(false)
const suggestionCatalog = ref<TicketSuggestionCatalog>({
  affectedFiles: [],
  assignees: [],
  dependencies: [],
  documentation: [],
  milestones: [],
  tags: [],
})
const ticketIssues = ref<TicketValidationIssue[]>([]);
const tickets = ref<Ticket[]>([])

const allTags = computed(() => (
  [...new Set(tickets.value.flatMap((ticket) => ticket.tags || []))].sort()
))

const filteredTickets = computed(() => {
  let result = tickets.value

  if (filter.value) {
    const query = filter.value.toLowerCase()
    result = result.filter((ticket) => (
      ticket.title.toLowerCase().includes(query)
      || ticket.tags.some((tag) => tag.includes(query))
      || formatId(ticket.id).toLowerCase().includes(query)
    ))
  }

  if (selectedTags.value.length > 0) {
    const selected = new Set(selectedTags.value)
    result = result.filter((ticket) => ticket.tags.some((tag) => selected.has(tag)))
  }

  return [...result].sort(compareTicketsByOrder)
})

const selectedTicket = computed(() => (
  tickets.value.find((ticket) => ticket.id === selectedId.value) || null
))

function columnTickets(key: string) {
  return filteredTickets.value
  .filter((ticket) => ticket.status === key)
  .sort(compareTicketsByOrder)
}

function fetchValidation() {
  if (!canEditBoard.value) {
    return
  }

  const params = new URLSearchParams({
    dir: ticketsDir.value,
    prefix: ticketPrefix.value,
  });

  for (const section of ticketSections.value) {
    params.append("section", section);
  }

  fetch(`/__vitepress_pm_validate?${params.toString()}`)
    .then((response) => response.ok ? response.json() : [])
      .then((data: TicketValidationIssue[]) => {
      ticketIssues.value = data
    })
    .catch(() => {
      ticketIssues.value = []
    })
}

function formatId(id: number): string {
  return ticketPrefix.value ? `${ticketPrefix.value}-${id}` : String(id)
}

function loadTickets() {
  const ticketUrl = import.meta.env.DEV
    ? `/__vitepress_pm_tickets?dir=${encodeURIComponent(ticketsDir.value)}`
    : `${import.meta.env.BASE_URL}__vitepress_pm_tickets/${encodeURIComponent(ticketsDir.value)}.json`
  const suggestionUrl = import.meta.env.DEV
    ? `/__vitepress_pm_suggestions?dir=${encodeURIComponent(ticketsDir.value)}&prefix=${encodeURIComponent(ticketPrefix.value)}`
    : `${import.meta.env.BASE_URL}__vitepress_pm_suggestions/${encodeURIComponent(ticketsDir.value)}.json`

  const ticketRequest = fetch(ticketUrl)
  .then((response) => {
    if (!response.ok) {
      throw new Error('Ticket data not available')
    }

    return response.json()
  });

  const suggestionRequest = fetch(suggestionUrl)
  .then((response) => {
    if (!response.ok) {
      throw new Error("Suggestion data not available")
    }

    return response.json()
  })
  .catch(() => ({
    affectedFiles: [],
    assignees: [],
    dependencies: [],
    documentation: [],
    milestones: [],
    tags: [],
  }));

  Promise.all([ticketRequest, suggestionRequest])
  .then(([ticketData, suggestions]) => {
    tickets.value = ticketData as Ticket[]
    suggestionCatalog.value = suggestions as TicketSuggestionCatalog
    fetchValidation()
  })
  .catch(() => {})
}

function onFixed() {
  showFixModal.value = false

  if (demo.value) {
    for (const issue of ticketIssues.value) {
      if (issue.type === "identity" && issue.fixedId && issue.fixedSlug) {
        const ticket = tickets.value.find((entry) => entry.id === issue.currentId || entry.id === 0);
        if (ticket) {
          ticket.id = issue.fixedId;
          ticket.url = `/${ticketsDir.value}/${issue.fixedSlug}.html`;
        }
      }
    }
    ticketIssues.value = []
    return
  }

  loadTickets()
}

function openNewTicket() {
  if (!canEditBoard.value) {
    return
  }

  draftTicket.value = {
    affectedFiles: [],
    assignee: "",
    dependencies: [],
    documentation: [],
    id: 0,
    milestone: "",
    title: 'New ticket',
    status: defaultColumn.value,
    priority: 'medium',
    tags: [],
    body: buildTicketTemplate(ticketSections.value),
    url: '',
  }
}

async function confirmCreate(draft: Ticket) {
  if (!canEditBoard.value) {
    return
  }

  try {
    const response = await fetch('/__vitepress_pm_create', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        dir: ticketsDir.value,
        prefix: ticketPrefix.value,
        status: draft.status,
        title: draft.title,
        priority: draft.priority,
        milestone: draft.milestone,
        dependencies: draft.dependencies,
        documentation: draft.documentation,
        affectedFiles: draft.affectedFiles,
        assignee: draft.assignee,
        tags: draft.tags,
        body: draft.body,
        sections: ticketSections.value,
      }),
    })

    if (!response.ok) {
      throw new Error(await response.text())
    }

    const ticket: Ticket = await response.json()
    tickets.value = [...tickets.value, ticket]
    draftTicket.value = null
  } catch (cause) {
    console.error('Failed to create ticket:', cause)
  }
}

function updateTicket(id: number, patch: Partial<Ticket>) {
  if (!canEditBoard.value) {
    return
  }

  tickets.value = tickets.value.map((ticket) => (
    ticket.id === id ? { ...ticket, ...patch } : ticket
  ))

  if (demo.value) {
    return
  }

  const ticket = tickets.value.find((entry) => entry.id === id)
  if (ticket?.url) {
    const { id: _id, url: _url, ...fields } = { ...patch } as Record<string, unknown>
    if (Object.keys(fields).length > 0) {
      writeTicket(ticket.url, fields)
    }
  }
}

async function persistReorder(updates: Array<{
  order: number
  status: string
  url: string
}>) {
  if (demo.value || updates.length === 0) {
    return
  }

  const reorderUpdates = updates.map((entry) => ({
    url: entry.url,
    updates: {
      order: entry.order,
      status: entry.status,
    },
  }))

  try {
    await writeTickets(reorderUpdates)
  } catch (cause) {
    console.error("Failed to persist ticket order:", cause)
    loadTickets()
  }
}

async function archiveSelectedTicket() {
  if (!canEditBoard.value || !selectedTicket.value?.url) {
    return
  }

  const confirmed = window.confirm(
    `Archive ${formatId(selectedTicket.value.id)}? You can restore it later from the archive board.`
  )
  if (!confirmed) {
    return
  }

  try {
    const archived = await archiveTicket(
      selectedTicket.value.url,
      archiveTicketsDir.value
    )
    if (archived) {
      selectedId.value = null
      loadTickets()
    }
  } catch (cause) {
    console.error("Failed to archive ticket:", cause)
  }
}

async function restoreSelectedTicket() {
  if (!canRestoreTickets.value || !selectedTicket.value?.url) {
    return
  }

  const confirmed = window.confirm(
    `Restore ${formatId(selectedTicket.value.id)} to the active board?`
  )
  if (!confirmed) {
    return
  }

  try {
    const restored = await restoreTicket(
      selectedTicket.value.url,
      restoreTicketsDir.value
    )
    if (restored) {
      selectedId.value = null
      loadTickets()
    }
  } catch (cause) {
    console.error("Failed to restore ticket:", cause)
  }
}

const {
  dragOverColumn,
  dragOverIndex,
  handleDragEnd,
  handleDragLeave,
  handleDragOver,
  handleDragStart,
  handleDrop,
} = useDragDrop((ticketId, targetColumn, targetIndex) => {
  if (!canEditBoard.value || hasActiveFilters.value) {
    return
  }

  const {reorderedTickets, updates} = reorderTickets({
    targetColumn,
    targetIndex,
    ticketId: Number(ticketId),
    tickets: tickets.value,
  })

  tickets.value = reorderedTickets
  void persistReorder(
    updates.filter((entry) => !!entry.url) as Array<{
      order: number
      status: string
      url: string
    }>
  )
})

if (typeof window !== 'undefined') {
  loadTickets()
}
</script>

<template>
  <div class="board-shell">
    <div
      v-if="readOnly"
      class="board-shell-notice"
      style="padding: 10px 16px; border-bottom: 1px solid rgba(237, 137, 54, 0.35); background: rgba(237, 137, 54, 0.08); color: #fbd38d; font-size: 12px; line-height: 1.4"
    >
      {{ READ_ONLY_BOARD_NOTICE }}
    </div>

    <div class="board-toolbar">
      <input
        class="board-filter-input"
        v-model="filter"
        placeholder="Filter tickets..."
        style="font-size: 12px; padding: 4px 10px; background: #171923; border: 1px solid #2d3748; border-radius: 5px; color: #e2e8f0; outline: none; width: 200px; height: 28px; box-sizing: border-box"
      >
      <TagFilterDropdown v-model="selectedTags" :tags="allTags" />
      <button
        v-if="canEditBoard && ticketIssues.length > 0"
        style="font-size: 12px; padding: 4px 12px; background: rgba(237, 137, 54, 0.12); border: 1px solid rgba(237, 137, 54, 0.4); border-radius: 5px; color: #ed8936; cursor: pointer; font-weight: 600; line-height: 1.2; height: 28px; box-sizing: border-box"
        @click="showFixModal = true"
      >&#9888; Fix {{ ticketIssues.length }} issue{{
          ticketIssues.length === 1 ? "" : "s"
        }}
      </button>
      <button
        v-if="canEditBoard"
        title="New ticket"
        style="font-size: 13px; padding: 4px 12px; background: #2d3748; border: 1px solid #4a5568; border-radius: 5px; color: #e2e8f0; cursor: pointer; font-weight: 600; line-height: 1.2; height: 28px; box-sizing: border-box"
        @click="openNewTicket"
      >+ New</button>
    </div>

    <div class="board-columns" style="flex: 1; display: flex; overflow: auto">
      <BoardColumn
        v-for="column in columns"
        :key="column.key"
        :active-drop-index="dragOverColumn === column.key ? dragOverIndex : null"
        :column="column"
        :is-over="dragOverColumn === column.key"
        :read-only="reorderReadOnly"
        :selected-id="selectedId"
        :ticket-prefix="ticketPrefix"
        :tickets="columnTickets(column.key)"
        @dragend="handleDragEnd"
        @dragleave="handleDragLeave"
        @dragover="(event: DragEvent, index: number) => handleDragOver(event, column.key, index)"
        @dragstart="(event: DragEvent, id: number) => handleDragStart(event, String(id))"
        @drop="(event: DragEvent, index: number) => handleDrop(event, column.key, index)"
        @select="(id: number) => selectedId = selectedId === id ? null : id"
      />
    </div>

    <TicketDetail
      v-if="draftTicket"
      :columns="columns"
      :read-only="ticketReadOnly"
      :suggestions="suggestionCatalog"
      :ticket="draftTicket"
        :ticket-sections="ticketSections"
      :ticket-prefix="ticketPrefix"
      create-mode
      @close="draftTicket = null"
      @create="confirmCreate(draftTicket)"
      @update="(_id: number, patch: Partial<Ticket>) => { draftTicket = { ...draftTicket!, ...patch } }"
    />

    <TicketDetail
      v-else-if="selectedTicket"
      :archive-enabled="canEditBoard"
      :columns="columns"
      :read-only="ticketReadOnly"
      :restore-enabled="canRestoreTickets"
      :suggestions="suggestionCatalog"
      :ticket="selectedTicket"
        :ticket-sections="ticketSections"
      :ticket-prefix="ticketPrefix"
      @archive="archiveSelectedTicket"
      @close="selectedId = null"
      @restore="restoreSelectedTicket"
      @update="updateTicket"
    />

    <TicketFixModal
      v-if="showFixModal"
      :demo="demo"
      :issues="ticketIssues"
      :ticket-prefix="ticketPrefix"
        :ticket-sections="ticketSections"
      :tickets-dir="ticketsDir"
      @close="showFixModal = false"
      @fixed="onFixed"
    />
  </div>
</template>
