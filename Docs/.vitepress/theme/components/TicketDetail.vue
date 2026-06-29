<script setup lang="ts">
import {computed, nextTick, onMounted, onUnmounted, ref, watch} from "vue";

import {
  COMPACT_TICKET_DETAIL_BREAKPOINT,
  isTicketDetailCompactViewport,
  shouldCollapseTicketDetailMetadataByDefault
} from "../../lib/ticket-detail-layout.mjs";
import {
  parseTicketSections,
  serializeTicketSections
} from "../../lib/ticket-sections.mjs";
import {
  buildDocumentationHref,
  resolveTicketHref
} from "../../lib/ticket-links.mjs";

import type {
  Column,
  SuggestionOption,
  Ticket,
  TicketSuggestionCatalog
} from "../types";
import {countCheckboxes, toggleCheckbox} from "../composables/useMarkdown";

import MarkdownBody from "./MarkdownBody.vue";
import ProgressBar from "./ProgressBar.vue";
import SuggestionCombobox from "./SuggestionCombobox.vue";
import TagEditor from "./TagEditor.vue";
import TokenSuggestionInput from "./TokenSuggestionInput.vue";

const props = withDefaults(defineProps<{
  archiveEnabled?: boolean
  columns: Column[]
  createMode?: boolean
  readOnly: boolean
  restoreEnabled?: boolean
  suggestions: TicketSuggestionCatalog
  ticket: Ticket
  ticketPrefix: string
  ticketSections: string[]
}>(), {
  archiveEnabled: false,
  createMode: false,
  restoreEnabled: false,
})

const emit = defineEmits<{
  archive: []
  close: []
  create: []
  restore: []
  update: [id: number, patch: Partial<Ticket>]
}>()

const priorityOptions = ['critical', 'high', 'medium', 'low'] as const
const editTitle = ref(false)
const isCompactMetadataViewport = ref(false)
const metadataCollapsed = ref(false)
const editingSection = ref<string | null>(null);
const sectionDraft = ref("");
const titleDraft = ref(props.ticket.title)
const titleRef = ref<HTMLInputElement | null>(null)
const metadataPanelId = "ticket-detail-metadata"
let compactViewportQuery: MediaQueryList | null = null

const column = computed(() => props.columns.find((entry) => entry.key === props.ticket.status))
const checks = computed(() => countCheckboxes(props.ticket.body))
const displayId = computed(() => (
  props.ticketPrefix ? `${props.ticketPrefix}-${props.ticket.id}` : String(props.ticket.id)
))
const filePath = computed(() => (
  props.ticket.url ? props.ticket.url.replace(/^\//, '').replace(/\.html$/, '.md') : ''
))
const parsedSections = computed(() => (
    parseTicketSections(props.ticket.body, props.ticketSections)
));
const currentTicketReference = computed(() => (
  props.ticket.id > 0
    ? (props.ticketPrefix
      ? `${props.ticketPrefix}-${props.ticket.id}`
      : String(props.ticket.id))
    : ""
))
const tagSuggestionOptions = computed<SuggestionOption[]>(() => (
  props.suggestions.tags.map((tag) => ({label: tag, value: tag}))
))
const assigneeSuggestionOptions = computed<SuggestionOption[]>(() => (
  props.suggestions.assignees.map((assignee) => ({
    label: assignee,
    value: assignee,
  }))
))
const milestoneSuggestionOptions = computed<SuggestionOption[]>(() => (
  props.suggestions.milestones.map((milestone) => ({
    label: milestone,
    value: milestone,
  }))
))
const dependencySuggestionOptions = computed<SuggestionOption[]>(() => (
  props.suggestions.dependencies.filter((option) => (
    option.value !== currentTicketReference.value
  ))
))
const documentationSuggestionOptions = computed<SuggestionOption[]>(() => (
  props.suggestions.documentation.map((entry) => ({
    label: entry,
    value: entry,
  }))
))
const ticketHrefLookup = computed<Record<string, string>>(() => (
  props.suggestions.dependencies.reduce<Record<string, string>>((lookup, option) => {
    if (option.url) {
      lookup[option.value] = option.url;
    }
    return lookup;
  }, {})
))
const affectedFileSuggestionOptions = computed<SuggestionOption[]>(() => (
  props.suggestions.affectedFiles.map((entry) => ({
    label: entry,
    value: entry,
  }))
))
const metadataToggleLabel = computed(() => (
  metadataCollapsed.value ? "Show details" : "Hide details"
))
const showMetadataPanel = computed(() => (
  !isCompactMetadataViewport.value || !metadataCollapsed.value
))
const modalCardStyle = computed(() => ({
  width: "calc(100vw - 20px)",
  maxWidth: "1300px",
  height: isCompactMetadataViewport.value ? "87vh" : "93vh",
  maxHeight: "1260px",
  background: "#0d1117",
  border: "1px solid #2d3748",
  borderRadius: "12px",
  display: "flex",
  flexDirection: "column",
  overflow: "hidden",
  boxShadow: "0 24px 48px rgba(0,0,0,0.4)",
}))
const modalBodyStyle = computed(() => ({
  flex: 1,
  display: "flex",
  overflow: "hidden",
  position: "relative",
}))
const contentPaneStyle = computed(() => ({
  flex: 1,
  overflowY: "auto",
  padding: isCompactMetadataViewport.value ? "12px" : "24px",
  minWidth: "0",
  scrollbarWidth: "thin",
}))
const metadataPaneStyle = computed(() => (
  isCompactMetadataViewport.value
    ? {
      position: "absolute",
      top: "0",
      right: "0",
      bottom: "0",
      width: isCompactMetadataViewport.value ? "100%" : "min(400px, calc(100vw - 40px))",
      maxWidth: "100%",
      borderLeft: "1px solid #2d3748",
      overflowY: "auto",
      padding: "12px",
      background: "#171923",
      scrollbarWidth: "thin",
      zIndex: "2",
      boxShadow: "-18px 0 30px rgba(0, 0, 0, 0.35)",
    }
    : {
      width: "400px",
      flexShrink: 0,
      borderLeft: "1px solid #2d3748",
      overflowY: "auto",
      padding: "12px",
      background: "#171923",
      scrollbarWidth: "thin",
    }
))

watch(() => props.ticket.id, () => {
  titleDraft.value = props.ticket.title
  editTitle.value = false
  editingSection.value = null;
  sectionDraft.value = "";
  syncMetadataViewportState(true)
})

watch(editTitle, (value) => {
  if (value) {
    nextTick(() => titleRef.value?.focus())
  }
})

function addTag(tag: string) {
  if (props.readOnly) {
    return
  }

  if (tag && !props.ticket.tags.includes(tag)) {
    emit('update', props.ticket.id, { tags: [...props.ticket.tags, tag] })
  }
}

function updateTags(tags: string[]) {
  if (props.readOnly) {
    return;
  }

  emit("update", props.ticket.id, {tags});
}

function updateAssignee(value: string) {
  if (props.readOnly) {
    return;
  }

  emit("update", props.ticket.id, {assignee: value.trim()});
}

function normalizeTagValue(value: string) {
  return `${value ?? ""}`.trim().toLowerCase();
}

function normalizeFilePathValue(value: string) {
  return `${value ?? ""}`.trim().replace(/\\/g, "/");
}

function syncMetadataViewportState(forceReset = false) {
  if (typeof window === "undefined") {
    return;
  }

  const compact = isTicketDetailCompactViewport(window.innerWidth);

  if (!forceReset && compact === isCompactMetadataViewport.value) {
    return;
  }

  isCompactMetadataViewport.value = compact;
  metadataCollapsed.value = compact
    ? shouldCollapseTicketDetailMetadataByDefault(window.innerWidth)
    : false;
}

function toggleMetadataCollapsed() {
  if (!isCompactMetadataViewport.value) {
    return;
  }

  metadataCollapsed.value = !metadataCollapsed.value;
}

function onCompactViewportChange() {
  syncMetadataViewportState()
}

function updateMilestone(value: string) {
  if (props.readOnly) {
    return;
  }

  emit("update", props.ticket.id, {milestone: value.trim()});
}

function updateTicketList(
    key: "dependencies" | "documentation" | "affectedFiles",
    value: string[]
) {
  if (props.readOnly) {
    return;
  }

  emit("update", props.ticket.id, {
    [key]: value,
  } as Partial<Ticket>);
}

function dependencyHref(value: string) {
  return resolveTicketHref(value, {
    ticketHrefs: ticketHrefLookup.value,
  });
}

function documentationHref(value: string) {
  return buildDocumentationHref(value);
}

function buildUpdatedBody(heading: string, content: string) {
  return serializeTicketSections(
      parsedSections.value.map((section) => (
          section.heading === heading
              ? {...section, content, missing: false}
              : section
      ))
  );
}

function onBackdropClick(event: MouseEvent) {
  if ((event.target as HTMLElement).classList.contains('ticket-modal-backdrop')) {
    emit('close')
  }
}

function onEscape(event: KeyboardEvent) {
  if (event.key !== "Escape" || editTitle.value || editingSection.value) {
    return;
  }

  if (isCompactMetadataViewport.value && showMetadataPanel.value) {
    metadataCollapsed.value = true;
    return;
  }

  emit('close')
}

function onSectionCheckboxToggle(heading: string, index: number) {
  if (props.readOnly) {
    return;
  }

  const section = parsedSections.value.find((entry) => entry.heading === heading);
  if (!section) {
    return;
  }

  emit("update", props.ticket.id, {
    body: buildUpdatedBody(heading, toggleCheckbox(section.content, index)),
  });
}

function removeTag(tag: string) {
  if (props.readOnly) {
    return
  }

  if (!tag) {
    if (props.ticket.tags.length > 0) {
      emit('update', props.ticket.id, { tags: props.ticket.tags.slice(0, -1) })
    }
    return
  }

  emit('update', props.ticket.id, { tags: props.ticket.tags.filter((entry) => entry !== tag) })
}

function saveSection() {
  if (!editingSection.value) {
    return;
  }

  if (!props.readOnly) {
    emit("update", props.ticket.id, {
      body: buildUpdatedBody(editingSection.value, sectionDraft.value),
    });
  }

  editingSection.value = null;
  sectionDraft.value = "";
}

function saveTitle() {
  if (!props.readOnly && titleDraft.value.trim()) {
    emit('update', props.ticket.id, { title: titleDraft.value.trim() })
  }
  editTitle.value = false
}

function startSectionEdit(heading: string, content: string) {
  if (props.readOnly) {
    return;
  }

  editingSection.value = heading;
  sectionDraft.value = content;
}

onMounted(() => {
  document.addEventListener('keydown', onEscape)
  syncMetadataViewportState(true)
  if (typeof window !== "undefined") {
    compactViewportQuery = window.matchMedia(
      `(max-width: ${COMPACT_TICKET_DETAIL_BREAKPOINT}px)`
    )
    compactViewportQuery.addEventListener("change", onCompactViewportChange)
  }
  if (props.createMode) {
    editTitle.value = true
  }
})

onUnmounted(() => {
  document.removeEventListener('keydown', onEscape)
  compactViewportQuery?.removeEventListener("change", onCompactViewportChange)
})
</script>

<template>
  <div
    class="ticket-modal-backdrop"
    style="position: fixed; inset: 0; z-index: 100; display: flex; align-items: center; justify-content: center; background: rgba(0, 0, 0, 0.6); backdrop-filter: blur(2px)"
    @click="onBackdropClick"
  >
    <div :style="modalCardStyle">
      <div style="display: flex; align-items: center; padding: 16px 24px; border-bottom: 1px solid #2d3748; flex-shrink: 0; background: #171923; gap: 12px">
        <span v-if="!createMode" style="font-size: 13px; font-weight: 700; color: #718096; font-family: monospace; white-space: nowrap; flex-shrink: 0">{{ displayId }}</span>
        <span v-else style="font-size: 13px; font-weight: 700; color: #6bcb6b; font-family: monospace; white-space: nowrap; flex-shrink: 0">NEW</span>

        <div v-if="editTitle" style="flex: 1; min-width: 0">
          <input
            ref="titleRef"
            v-model="titleDraft"
            style="width: 100%; font-size: 18px; font-weight: 700; color: #e2e8f0; background: #0d1117; border: 1px solid #e6a817; border-radius: 4px; padding: 4px 10px; outline: none"
            @blur="saveTitle"
            @keydown.enter="saveTitle"
            @keydown.escape.stop="editTitle = false"
          >
        </div>
        <h2
          v-else
          style="margin: 0; font-size: 18px; font-weight: 700; color: #e2e8f0; flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap"
        >{{ ticket.title }}</h2>

        <span
          v-if="restoreEnabled"
          style="font-size: 11px; color: #90cdf4; background: rgba(99, 179, 237, 0.12); border: 1px solid rgba(99, 179, 237, 0.35); border-radius: 999px; padding: 4px 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.6px"
        >Archived</span>

        <span
          v-else-if="readOnly"
          style="font-size: 11px; color: #ed8936; background: rgba(237, 137, 54, 0.12); border: 1px solid rgba(237, 137, 54, 0.4); border-radius: 999px; padding: 4px 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.6px"
        >Read only</span>

        <button
          v-if="isCompactMetadataViewport"
          :aria-controls="metadataPanelId"
          :aria-expanded="showMetadataPanel ? 'true' : 'false'"
          :title="metadataToggleLabel"
          style="background: none; border: 1px solid #2d3748; color: #cbd5e0; cursor: pointer; font-size: 12px; padding: 4px 10px; border-radius: 999px; flex-shrink: 0; line-height: 1.2"
          @click="toggleMetadataCollapsed"
        >{{ metadataToggleLabel }}</button>

        <button
          v-if="!editTitle && !readOnly"
          title="Edit title"
          style="background: none; border: 1px solid #2d3748; color: #718096; cursor: pointer; font-size: 13px; padding: 4px 8px; border-radius: 4px; flex-shrink: 0; line-height: 1"
          @click="titleDraft = ticket.title; editTitle = true"
        >&#9998;</button>

        <button
          style="background: none; border: none; color: #718096; cursor: pointer; font-size: 20px; padding: 4px 8px; flex-shrink: 0; line-height: 1"
          @click="emit('close')"
        >&times;</button>
      </div>

      <div :style="modalBodyStyle">
        <div
          v-if="isCompactMetadataViewport && showMetadataPanel"
          style="position: absolute; inset: 0; background: rgba(0, 0, 0, 0.16); z-index: 1"
          @click="metadataCollapsed = true"
        />

        <div :style="contentPaneStyle">
          <div v-if="checks.total > 0" style="margin-bottom: 20px">
            <ProgressBar :color="column?.color || '#718096'" :done="checks.done" :total="checks.total" />
          </div>

          <div
              v-for="section in parsedSections"
              :key="section.heading"
              style="margin-bottom: 28px; padding-bottom: 24px; border-bottom: 1px solid #1a202c"
          >
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; gap: 12px">
              <span style="font-size: 12px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700">{{
                  section.heading
                }}</span>
              <button
                  v-if="editingSection !== section.heading && !readOnly"
                  style="background: none; border: 1px solid #2d3748; color: #718096; cursor: pointer; font-size: 13px; padding: 4px 8px; border-radius: 4px; line-height: 1"
                  title="Edit section"
                  @click="startSectionEdit(section.heading, section.content)"
              >&#9998;
              </button>
              <div
                  v-else-if="editingSection === section.heading"
                  style="display: flex; gap: 6px">
                <button
                    style="font-size: 12px; color: #6bcb6b; background: rgba(107, 203, 107, 0.09); border: 1px solid rgba(107, 203, 107, 0.27); border-radius: 4px; padding: 3px 12px; cursor: pointer"
                    @click="saveSection"
                >Save
                </button>
                <button
                    style="font-size: 12px; color: #718096; background: none; border: 1px solid #2d3748; border-radius: 4px; padding: 3px 12px; cursor: pointer"
                    @click="editingSection = null; sectionDraft = ''"
                >Cancel
                </button>
              </div>
            </div>

            <textarea
                v-if="editingSection === section.heading"
                v-model="sectionDraft"
                placeholder="Markdown here... Use - [ ] for checkboxes"
                style="width: 100%; min-height: 180px; padding: 12px; font-size: 13px; background: #171923; color: #e2e8f0; border: 1px solid rgba(230, 168, 23, 0.27); border-radius: 6px; resize: vertical; font-family: 'JetBrains Mono', monospace; line-height: 1.6; outline: none; box-sizing: border-box"
            />
            <p
                v-else-if="!section.content.trim()"
                style="font-size: 13px; color: #718096; font-style: italic; margin: 0"
            >No content yet.</p>
            <MarkdownBody
                v-else
                :interactive="!readOnly"
                :text="section.content"
                @checkbox-toggle="(index: number) => onSectionCheckboxToggle(section.heading, index)"
            />
          </div>
        </div>

        <div
          v-show="showMetadataPanel"
          :id="metadataPanelId"
          :style="metadataPaneStyle"
        >
          <p
            v-if="readOnly"
            style="font-size: 12px; color: #a0aec0; line-height: 1.5; margin-top: 0; margin-bottom: 20px"
          >
            {{ restoreEnabled
              ? "Archived tickets are read-only. Use Restore to move this ticket back to the active board."
              : "Open this board through the local docs server to make persistent changes from the webpage." }}
          </p>

          <div style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">Status</label>
            <div style="position: relative">
              <select
                :disabled="readOnly"
                :value="ticket.status"
                style="width: 100%; appearance: none; font-size: 13px; padding: 7px 32px 7px 10px; background: #0d1117; border: 1px solid #2d3748; border-radius: 6px; color: #e2e8f0; outline: none"
                :style="{ cursor: readOnly ? 'default' : 'pointer', opacity: readOnly ? 0.8 : 1 }"
                @change="emit('update', ticket.id, { status: ($event.target as HTMLSelectElement).value })"
              >
                <option
                  v-for="entry in columns"
                  :key="entry.key"
                  :value="entry.key"
                >{{ entry.label }}</option>
              </select>
              <div style="position: absolute; right: 10px; top: 50%; transform: translateY(-50%); pointer-events: none; color: #718096; font-size: 10px">&#9660;</div>
            </div>
          </div>

          <div style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">Priority</label>
            <div style="position: relative">
              <select
                :disabled="readOnly"
                :value="ticket.priority"
                style="width: 100%; appearance: none; font-size: 13px; padding: 7px 32px 7px 10px; background: #0d1117; border: 1px solid #2d3748; border-radius: 6px; color: #e2e8f0; outline: none"
                :style="{ cursor: readOnly ? 'default' : 'pointer', opacity: readOnly ? 0.8 : 1 }"
                @change="emit('update', ticket.id, { priority: ($event.target as HTMLSelectElement).value as Ticket['priority'] })"
              >
                <option
                  v-for="priority in priorityOptions"
                  :key="priority"
                  :value="priority"
                >{{ priority.charAt(0).toUpperCase() + priority.slice(1) }}</option>
              </select>
              <div style="position: absolute; right: 10px; top: 50%; transform: translateY(-50%); pointer-events: none; color: #718096; font-size: 10px">&#9660;</div>
            </div>
          </div>

          <div style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">Tags</label>
            <TokenSuggestionInput
                v-if="!readOnly"
                :model-value="ticket.tags"
                :normalize-value="normalizeTagValue"
                :options="tagSuggestionOptions"
                placeholder="Add tag..."
                @update:modelValue="updateTags"
            />
            <TagEditor
                v-else
                :read-only="readOnly"
                :tags="ticket.tags"
                @add="addTag"
                @remove="removeTag"
            />
          </div>

          <div style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">Assignee</label>
            <SuggestionCombobox
                v-if="!readOnly"
                :model-value="ticket.assignee"
                :options="assigneeSuggestionOptions"
                placeholder="Assign one person..."
                @update:modelValue="updateAssignee"
            />
            <div
                v-else
                style="font-size: 12px; color: #cbd5e0; padding: 7px 10px; background: #0d1117; border: 1px solid #2d3748; border-radius: 6px"
            >{{ ticket.assignee || "Unassigned" }}
            </div>
          </div>

          <div style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">Milestone</label>
            <SuggestionCombobox
                v-if="!readOnly"
                :model-value="ticket.milestone"
                :options="milestoneSuggestionOptions"
                placeholder="e.g. m-0"
                @update:modelValue="updateMilestone"
            />
            <div
                v-else
                style="font-size: 12px; color: #cbd5e0; padding: 7px 10px; background: #0d1117; border: 1px solid #2d3748; border-radius: 6px"
            >{{ ticket.milestone || "No milestone" }}
            </div>
          </div>

          <div style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">Dependencies</label>
            <TokenSuggestionInput
                v-if="!readOnly"
                :model-value="ticket.dependencies"
                :options="dependencySuggestionOptions"
                placeholder="Add dependency..."
                @update:modelValue="updateTicketList('dependencies', $event)"
            />
            <div
                v-if="ticket.dependencies.length > 0"
                style="display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px">
              <a
                  v-for="dependency in ticket.dependencies"
                  :key="dependency"
                  :href="dependencyHref(dependency) || undefined"
                  style="font-size: 11px; padding: 3px 8px; border-radius: 999px; background: rgba(99, 179, 237, 0.12); border: 1px solid rgba(99, 179, 237, 0.3); color: #90cdf4; text-decoration: none"
              >{{ dependency }}</a>
            </div>
            <div
                v-else-if="readOnly"
                style="font-size: 12px; color: #718096"
            >No dependencies
            </div>
          </div>

          <div style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">Documentation</label>
            <TokenSuggestionInput
                v-if="!readOnly"
                :model-value="ticket.documentation"
                :options="documentationSuggestionOptions"
                placeholder="Add doc path..."
                @update:modelValue="updateTicketList('documentation', $event)"
            />
            <div
                v-if="ticket.documentation.length > 0"
                style="display: flex; flex-direction: column; gap: 6px; margin-top: 8px">
              <component
                  v-for="entry in ticket.documentation"
                  :key="entry"
                  :is="documentationHref(entry) ? 'a' : 'span'"
                  :href="documentationHref(entry) || undefined"
                  :style="documentationHref(entry)
                  ? 'font-size: 12px; color: #90cdf4; text-decoration: none; line-height: 1.4'
                  : 'font-size: 12px; color: #cbd5e0; line-height: 1.4; font-family: monospace'"
              >{{ entry }}
              </component>
            </div>
            <div
                v-else-if="readOnly"
                style="font-size: 12px; color: #718096"
            >No documentation links
            </div>
          </div>

          <div style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">Likely
              Affected Files</label>
            <TokenSuggestionInput
                v-if="!readOnly"
                :model-value="ticket.affectedFiles"
                :normalize-value="normalizeFilePathValue"
                :options="affectedFileSuggestionOptions"
                placeholder="Add repo path..."
                @update:modelValue="updateTicketList('affectedFiles', $event)"
            />
            <div
                v-if="ticket.affectedFiles.length > 0"
                style="display: flex; flex-direction: column; gap: 6px; margin-top: 8px">
              <span
                  v-for="entry in ticket.affectedFiles"
                  :key="entry"
                  style="font-size: 12px; color: #cbd5e0; line-height: 1.4; font-family: monospace"
              >{{ entry }}</span>
            </div>
            <div
                v-else-if="readOnly"
                style="font-size: 12px; color: #718096"
            >No affected files
            </div>
          </div>

          <div v-if="!createMode && filePath" style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">File</label>
            <span style="font-size: 12px; color: #4a5568; font-family: monospace; word-break: break-all">{{ filePath }}</span>
          </div>

          <div
            v-if="!createMode && (archiveEnabled || restoreEnabled)"
            style="padding-top: 20px; margin-bottom: 20px; border-top: 1px solid #2d3748"
          >
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 8px">Actions</label>
            <button
              v-if="archiveEnabled"
              style="font-size: 12px; color: #feb2b2; background: rgba(245, 101, 101, 0.09); border: 1px solid rgba(245, 101, 101, 0.27); border-radius: 6px; padding: 6px 14px; cursor: pointer; width: 100%; font-weight: 600"
              @click="emit('archive')"
            >Archive ticket</button>
            <button
              v-else-if="restoreEnabled"
              style="font-size: 12px; color: #90cdf4; background: rgba(99, 179, 237, 0.09); border: 1px solid rgba(99, 179, 237, 0.27); border-radius: 6px; padding: 6px 14px; cursor: pointer; width: 100%; font-weight: 600"
              @click="emit('restore')"
            >Restore ticket</button>
          </div>

          <div v-if="createMode" style="padding-top: 20px; border-top: 1px solid #2d3748">
            <button
              style="font-size: 12px; color: #6bcb6b; background: rgba(107, 203, 107, 0.09); border: 1px solid rgba(107, 203, 107, 0.27); border-radius: 6px; padding: 6px 14px; cursor: pointer; width: 100%; font-weight: 600"
              @click="emit('create')"
            >Create ticket</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
