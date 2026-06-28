<script setup lang="ts">
import { computed } from 'vue'

import type { Ticket } from '../types'
import { countCheckboxes } from '../composables/useMarkdown'

import ProgressBar from './ProgressBar.vue'

const props = defineProps<{
  color: string
  readOnly: boolean
  selected: boolean
  ticket: Ticket
  ticketPrefix: string
}>()

defineEmits<{
  dragend: [event: DragEvent]
  dragstart: [event: DragEvent]
  select: []
}>()

const priorityColors: Record<string, string> = {
  critical: '#f56565',
  high: '#ed8936',
  medium: '#ecc94b',
  low: '#68d391',
}

const checks = computed(() => countCheckboxes(props.ticket.body))
const displayId = computed(() => (
  props.ticketPrefix ? `${props.ticketPrefix}-${props.ticket.id}` : String(props.ticket.id)
))
</script>

<template>
  <div
    :draggable="!readOnly"
    :style="{
      background: selected ? '#2d3748' : '#1a202c',
      border: selected ? `2px solid ${color}` : '1px solid #2d3748',
      borderRadius: '7px',
      padding: '10px 12px',
      cursor: readOnly ? 'pointer' : 'grab',
      userSelect: 'none',
      transition: 'border-color 0.15s',
    }"
    @click="$emit('select')"
    @dragend="$emit('dragend', $event)"
    @dragstart="$emit('dragstart', $event)"
  >
    <div style="display: flex; align-items: center; gap: 6px; margin-bottom: 4px">
      <span style="font-size: 10px; color: #4a5568; font-family: monospace; font-weight: 600">{{ displayId }}</span>
      <div
        :style="{
          width: '6px',
          height: '6px',
          borderRadius: '50%',
          background: priorityColors[ticket.priority] || '#718096',
          flexShrink: 0,
        }"
      />
    </div>
    <div style="font-size: 13px; font-weight: 600; color: #e2e8f0; line-height: 1.3">{{ ticket.title }}</div>
    <div v-if="ticket.tags.length" style="display: flex; flex-wrap: wrap; gap: 3px; margin-top: 6px">
      <span
        v-for="tag in ticket.tags"
        :key="tag"
        style="font-size: 10px; padding: 1px 6px; border-radius: 8px; background: #2d3748; color: #718096"
      >{{ tag }}</span>
    </div>
    <div v-if="checks.total > 0" style="margin-top: 6px">
      <ProgressBar :color="color" :done="checks.done" :total="checks.total" />
    </div>
  </div>
</template>
