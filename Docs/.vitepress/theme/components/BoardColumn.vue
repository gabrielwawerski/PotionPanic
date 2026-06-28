<script setup lang="ts">
import type { Column, Ticket } from '../types'

import BoardCard from './BoardCard.vue'

const props = defineProps<{
  column: Column
  isOver: boolean
  readOnly: boolean
  selectedId: number | null
  ticketPrefix: string
  tickets: Ticket[]
}>()

const emit = defineEmits<{
  dragend: [event: DragEvent]
  dragleave: []
  dragover: [event: DragEvent]
  dragstart: [event: DragEvent, id: number]
  drop: [event: DragEvent]
  select: [id: number]
}>()

function emitIfWritable(type: 'dragover' | 'drop' | 'dragleave', event?: DragEvent) {
  if (props.readOnly) {
    return
  }

  if (type === 'dragover' && event) {
    emit('dragover', event)
  } else if (type === 'drop' && event) {
    emit('drop', event)
  } else {
    emit('dragleave')
  }
}
</script>

<template>
  <div
    :style="{
      flex: 1,
      minWidth: '190px',
      display: 'flex',
      flexDirection: 'column',
      margin: '0 4px',
      borderRadius: '8px',
      padding: '6px',
      background: isOver ? `${column.color}0d` : 'transparent',
      border: isOver ? `2px dashed ${column.color}55` : '2px solid transparent',
      transition: 'all 0.15s',
    }"
    @dragover.prevent="emitIfWritable('dragover', $event)"
    @dragleave="emitIfWritable('dragleave')"
    @drop.prevent="emitIfWritable('drop', $event)"
  >
    <div
      :style="{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '4px 8px',
        marginBottom: '8px',
        borderBottom: `2px solid ${column.color}`,
      }"
    >
      <span
        :style="{
          fontSize: '11px',
          fontWeight: 700,
          color: column.color,
          textTransform: 'uppercase',
          letterSpacing: '1px',
        }"
      >{{ column.label }}</span>
      <span
        :style="{
          fontSize: '11px',
          color: column.color,
          background: `${column.color}18`,
          padding: '1px 7px',
          borderRadius: '10px',
          fontWeight: 600,
        }"
      >{{ tickets.length }}</span>
    </div>

    <div class="board-column-cards" style="flex: 1; display: flex; flex-direction: column; gap: 6px; overflow-y: auto; padding-bottom: 12px">
      <BoardCard
        v-for="ticket in tickets"
        :key="ticket.id"
        :color="column.color"
        :read-only="readOnly"
        :selected="selectedId === ticket.id"
        :ticket="ticket"
        :ticket-prefix="ticketPrefix"
        @dragend="$emit('dragend', $event)"
        @dragstart="$emit('dragstart', $event, ticket.id)"
        @select="$emit('select', ticket.id)"
      />
      <div
        v-if="tickets.length === 0"
        style="padding: 24px; text-align: center; color: rgba(45, 55, 72, 0.4); font-size: 12px; border: 1px dashed #2d3748; border-radius: 8px"
      >{{ readOnly ? 'No tickets' : 'Drop here' }}</div>
    </div>
  </div>
</template>
