<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'

import type { Column, Ticket } from '../types'
import { countCheckboxes, toggleCheckbox } from '../composables/useMarkdown'

import MarkdownBody from './MarkdownBody.vue'
import ProgressBar from './ProgressBar.vue'
import TagEditor from './TagEditor.vue'

const props = withDefaults(defineProps<{
  columns: Column[]
  createMode?: boolean
  readOnly: boolean
  ticket: Ticket
  ticketPrefix: string
}>(), {
  createMode: false,
})

const emit = defineEmits<{
  close: []
  create: []
  update: [id: number, patch: Partial<Ticket>]
}>()

const priorityOptions = ['critical', 'high', 'medium', 'low'] as const
const editing = ref(false)
const draft = ref(props.ticket.body)
const editTitle = ref(false)
const titleDraft = ref(props.ticket.title)
const titleRef = ref<HTMLInputElement | null>(null)

const column = computed(() => props.columns.find((entry) => entry.key === props.ticket.status))
const checks = computed(() => countCheckboxes(props.ticket.body))
const displayId = computed(() => (
  props.ticketPrefix ? `${props.ticketPrefix}-${props.ticket.id}` : String(props.ticket.id)
))
const filePath = computed(() => (
  props.ticket.url ? props.ticket.url.replace(/^\//, '').replace(/\.html$/, '.md') : ''
))

watch(() => props.ticket.id, () => {
  draft.value = props.ticket.body
  titleDraft.value = props.ticket.title
  editing.value = false
  editTitle.value = false
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

function onBackdropClick(event: MouseEvent) {
  if ((event.target as HTMLElement).classList.contains('ticket-modal-backdrop')) {
    emit('close')
  }
}

function onCheckboxToggle(index: number) {
  if (!props.readOnly) {
    emit('update', props.ticket.id, { body: toggleCheckbox(props.ticket.body, index) })
  }
}

function onEscape(event: KeyboardEvent) {
  if (event.key === 'Escape' && !editTitle.value && !editing.value) {
    emit('close')
  }
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

function saveBody() {
  if (!props.readOnly) {
    emit('update', props.ticket.id, { body: draft.value })
  }
  editing.value = false
}

function saveTitle() {
  if (!props.readOnly && titleDraft.value.trim()) {
    emit('update', props.ticket.id, { title: titleDraft.value.trim() })
  }
  editTitle.value = false
}

onMounted(() => {
  document.addEventListener('keydown', onEscape)
  if (props.createMode) {
    editTitle.value = true
  }
})

onUnmounted(() => document.removeEventListener('keydown', onEscape))
</script>

<template>
  <div
    class="ticket-modal-backdrop"
    style="position: fixed; inset: 0; z-index: 100; display: flex; align-items: center; justify-content: center; background: rgba(0, 0, 0, 0.6); backdrop-filter: blur(2px)"
    @click="onBackdropClick"
  >
    <div style="width: 90vw; max-width: 960px; height: 80vh; max-height: 700px; background: #0d1117; border: 1px solid #2d3748; border-radius: 12px; display: flex; flex-direction: column; overflow: hidden; box-shadow: 0 24px 48px rgba(0,0,0,0.4)">
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
          v-if="readOnly"
          style="font-size: 11px; color: #ed8936; background: rgba(237, 137, 54, 0.12); border: 1px solid rgba(237, 137, 54, 0.4); border-radius: 999px; padding: 4px 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.6px"
        >Read only</span>

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

      <div style="flex: 1; display: flex; overflow: hidden">
        <div style="flex: 1; overflow-y: auto; padding: 24px; min-width: 0">
          <div v-if="checks.total > 0" style="margin-bottom: 16px">
            <ProgressBar :color="column?.color || '#718096'" :done="checks.done" :total="checks.total" />
          </div>

          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px">
            <span style="font-size: 12px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700">Description</span>
            <button
              v-if="!editing && !readOnly"
              title="Edit description"
              style="background: none; border: 1px solid #2d3748; color: #718096; cursor: pointer; font-size: 13px; padding: 4px 8px; border-radius: 4px; line-height: 1"
              @click="draft = ticket.body; editing = true"
            >&#9998;</button>
            <div v-else-if="editing" style="display: flex; gap: 6px">
              <button
                style="font-size: 12px; color: #6bcb6b; background: rgba(107, 203, 107, 0.09); border: 1px solid rgba(107, 203, 107, 0.27); border-radius: 4px; padding: 3px 12px; cursor: pointer"
                @click="saveBody"
              >Save</button>
              <button
                style="font-size: 12px; color: #718096; background: none; border: 1px solid #2d3748; border-radius: 4px; padding: 3px 12px; cursor: pointer"
                @click="editing = false"
              >Cancel</button>
            </div>
          </div>

          <textarea
            v-if="editing"
            v-model="draft"
            placeholder="Markdown here... Use - [ ] for checkboxes"
            style="width: 100%; min-height: 300px; padding: 12px; font-size: 13px; background: #171923; color: #e2e8f0; border: 1px solid rgba(230, 168, 23, 0.27); border-radius: 6px; resize: vertical; font-family: 'JetBrains Mono', monospace; line-height: 1.6; outline: none; box-sizing: border-box"
          />
          <MarkdownBody v-else :interactive="!readOnly" :text="ticket.body" @checkbox-toggle="onCheckboxToggle" />
        </div>

        <div style="width: 280px; flex-shrink: 0; border-left: 1px solid #2d3748; overflow-y: auto; padding: 24px; background: #171923">
          <p
            v-if="readOnly"
            style="font-size: 12px; color: #a0aec0; line-height: 1.5; margin-top: 0; margin-bottom: 20px"
          >
            Open this board through the local docs server to make persistent changes from the webpage.
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
            <TagEditor :read-only="readOnly" :tags="ticket.tags" @add="addTag" @remove="removeTag" />
          </div>

          <div v-if="!createMode && filePath" style="margin-bottom: 20px">
            <label style="display: block; font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700; margin-bottom: 6px">File</label>
            <span style="font-size: 12px; color: #4a5568; font-family: monospace; word-break: break-all">{{ filePath }}</span>
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
