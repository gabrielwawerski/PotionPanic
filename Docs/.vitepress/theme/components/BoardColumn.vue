<script setup lang="ts">
import type { Column, Ticket } from '../types'

import BoardCard from './BoardCard.vue'

const props = defineProps<{
  activeDropIndex: number | null
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
  dragover: [event: DragEvent, index: number]
  dragstart: [event: DragEvent, id: number]
  drop: [event: DragEvent, index: number]
  select: [id: number]
}>()

function emitIfWritable(
  type: 'dragover' | 'drop' | 'dragleave',
  event?: DragEvent,
  index?: number
) {
  if (props.readOnly) {
    return
  }

  if (type === 'dragover' && event && typeof index === 'number') {
    emit('dragover', event, index)
  } else if (type === 'drop' && event && typeof index === 'number') {
    emit('drop', event, index)
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
    @dragleave="emitIfWritable('dragleave')"
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
      <div
        v-if="tickets.length === 0"
        :style="{
          minHeight: '52px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          textAlign: 'center',
          color: 'rgba(45, 55, 72, 0.72)',
          fontSize: '12px',
          border: activeDropIndex === 0
            ? `1px dashed ${column.color}`
            : '1px dashed #2d3748',
          borderRadius: '8px',
          background: activeDropIndex === 0 ? `${column.color}18` : 'transparent',
          transition: 'all 0.15s',
        }"
        @dragover.prevent="emitIfWritable('dragover', $event, 0)"
        @dragleave="emitIfWritable('dragleave')"
        @drop.prevent="emitIfWritable('drop', $event, 0)"
      >{{ readOnly ? 'No tickets' : 'Drop here' }}</div>
      <div
        v-for="(ticket, index) in tickets"
        :key="`${ticket.id}-slot`"
        style="display: contents"
      >
        <div
          :style="{
            height: activeDropIndex === index ? '16px' : '6px',
            marginBottom: '2px',
            borderRadius: '999px',
            background: activeDropIndex === index ? `${column.color}55` : 'transparent',
            transition: 'all 0.15s',
          }"
          @dragover.prevent="emitIfWritable('dragover', $event, index)"
          @dragleave="emitIfWritable('dragleave')"
          @drop.prevent="emitIfWritable('drop', $event, index)"
        />
        <BoardCard
          :color="column.color"
          :read-only="readOnly"
          :selected="selectedId === ticket.id"
          :ticket="ticket"
          :ticket-prefix="ticketPrefix"
          @dragend="$emit('dragend', $event)"
          @dragstart="$emit('dragstart', $event, ticket.id)"
          @select="$emit('select', ticket.id)"
        />
      </div>
      <div
        v-if="tickets.length > 0"
        :style="{
          height: activeDropIndex === tickets.length ? '16px' : '6px',
          marginTop: '2px',
          borderRadius: '999px',
          background: activeDropIndex === tickets.length ? `${column.color}55` : 'transparent',
          transition: 'all 0.15s',
        }"
        @dragover.prevent="emitIfWritable('dragover', $event, tickets.length)"
        @dragleave="emitIfWritable('dragleave')"
        @drop.prevent="emitIfWritable('drop', $event, tickets.length)"
      />
    </div>
  </div>
</template>
