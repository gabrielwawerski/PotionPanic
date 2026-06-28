<script setup lang="ts">
import {computed, ref} from "vue";
import {useData} from "vitepress";

import {
  buildTicketTemplate,
  normalizeTicketSections
} from "../../lib/ticket-sections.mjs";

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
const { writeTicket } = useTicketWriter()

const columns = computed<Column[]>(() => frontmatter.value.columns || [])
const defaultColumn = computed(() => (
  frontmatter.value.defaultColumn || (columns.value.length > 0 ? columns.value[0].key : 'backlog')
))
const demo = computed(() => !!frontmatter.value.demo)
const ticketPrefix = computed(() => frontmatter.value.ticketPrefix || '')
const ticketsDir = computed(() => frontmatter.value.ticketsDir || 'tickets')
const ticketSections = computed<string[]>(() => (
    normalizeTicketSections(frontmatter.value.ticketSections || [])
));
const readOnly = computed(() => !import.meta.env.DEV)

const draftTicket = ref<Ticket | null>(null)
const filter = ref('')
const selectedId = ref<number | null>(null)
const selectedTags = ref<string[]>([])
const showFixModal = ref(false)
const suggestionCatalog = ref<TicketSuggestionCatalog>({
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

  return [...result].sort((left, right) => left.id - right.id)
})

const selectedTicket = computed(() => (
  tickets.value.find((ticket) => ticket.id === selectedId.value) || null
))

function columnTickets(key: string) {
  return filteredTickets.value.filter((ticket) => ticket.status === key)
}

function fetchValidation() {
  if (readOnly.value) {
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
  if (readOnly.value) {
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
  if (readOnly.value) {
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
  if (readOnly.value) {
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

const {
  dragOverColumn,
  handleDragEnd,
  handleDragLeave,
  handleDragOver,
  handleDragStart,
  handleDrop,
} = useDragDrop((ticketId, targetColumn) => {
  updateTicket(Number(ticketId), { status: targetColumn })
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
      This published board is read-only. Run <code>npm run docs:dev</code> or <code>.\Scripts\docs-ui.ps1</code> to edit tasks from the webpage and write changes back to markdown.
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
        v-if="!readOnly && ticketIssues.length > 0"
        style="font-size: 12px; padding: 4px 12px; background: rgba(237, 137, 54, 0.12); border: 1px solid rgba(237, 137, 54, 0.4); border-radius: 5px; color: #ed8936; cursor: pointer; font-weight: 600; line-height: 1.2; height: 28px; box-sizing: border-box"
        @click="showFixModal = true"
      >&#9888; Fix {{ ticketIssues.length }} issue{{
          ticketIssues.length === 1 ? "" : "s"
        }}
      </button>
      <button
        v-if="!readOnly"
        title="New ticket"
        style="font-size: 13px; padding: 4px 12px; background: #2d3748; border: 1px solid #4a5568; border-radius: 5px; color: #e2e8f0; cursor: pointer; font-weight: 600; line-height: 1.2; height: 28px; box-sizing: border-box"
        @click="openNewTicket"
      >+ New</button>
    </div>

    <div class="board-columns" style="flex: 1; display: flex; overflow: auto">
      <BoardColumn
        v-for="column in columns"
        :key="column.key"
        :column="column"
        :is-over="dragOverColumn === column.key"
        :read-only="readOnly"
        :selected-id="selectedId"
        :ticket-prefix="ticketPrefix"
        :tickets="columnTickets(column.key)"
        @dragend="handleDragEnd"
        @dragleave="handleDragLeave"
        @dragover="(event: DragEvent) => handleDragOver(event, column.key)"
        @dragstart="(event: DragEvent, id: number) => handleDragStart(event, String(id))"
        @drop="(event: DragEvent) => handleDrop(event, column.key)"
        @select="(id: number) => selectedId = selectedId === id ? null : id"
      />
    </div>

    <TicketDetail
      v-if="draftTicket"
      :columns="columns"
      :read-only="readOnly"
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
      :columns="columns"
      :read-only="readOnly"
      :suggestions="suggestionCatalog"
      :ticket="selectedTicket"
        :ticket-sections="ticketSections"
      :ticket-prefix="ticketPrefix"
      @close="selectedId = null"
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
