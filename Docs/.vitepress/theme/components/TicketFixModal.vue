<script setup lang="ts">
import { ref } from 'vue'

interface TicketIssue {
  currentId: number
  currentSlug: string
  file: string
  fixedId: number
  fixedSlug: string
}

const props = defineProps<{
  demo?: boolean
  issues: TicketIssue[]
  ticketPrefix: string
  ticketsDir: string
}>()

const emit = defineEmits<{
  close: []
  fixed: []
}>()

const fixing = ref(false)

async function fix() {
  fixing.value = true

  try {
    if (!props.demo) {
      const response = await fetch('/__vitepress_pm_fix', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ dir: props.ticketsDir, prefix: props.ticketPrefix }),
      })

      if (!response.ok) {
        throw new Error(await response.text())
      }
    }

    emit('fixed')
  } catch (cause) {
    console.error('Failed to fix tickets:', cause)
  } finally {
    fixing.value = false
  }
}

function onBackdropClick(event: MouseEvent) {
  if ((event.target as HTMLElement).classList.contains('fix-modal-backdrop')) {
    emit('close')
  }
}
</script>

<template>
  <div
    class="fix-modal-backdrop"
    style="position: fixed; inset: 0; z-index: 100; display: flex; align-items: center; justify-content: center; background: rgba(0, 0, 0, 0.6); backdrop-filter: blur(2px)"
    @click="onBackdropClick"
  >
    <div style="width: 90vw; max-width: 640px; max-height: 80vh; background: #0d1117; border: 1px solid #2d3748; border-radius: 12px; display: flex; flex-direction: column; overflow: hidden; box-shadow: 0 24px 48px rgba(0,0,0,0.4)">
      <div style="display: flex; align-items: center; padding: 16px 24px; border-bottom: 1px solid #2d3748; background: #171923; gap: 12px">
        <span style="font-size: 16px; font-weight: 700; color: #ed8936; flex: 1">&#9888; Fix {{ issues.length }} ticket{{ issues.length === 1 ? '' : 's' }}</span>
        <button
          style="background: none; border: none; color: #718096; cursor: pointer; font-size: 20px; padding: 4px 8px; line-height: 1"
          @click="emit('close')"
        >&times;</button>
      </div>

      <div style="flex: 1; overflow-y: auto; padding: 16px 24px">
        <div
          v-for="issue in issues"
          :key="issue.file"
          style="display: flex; align-items: center; gap: 12px; padding: 10px 0; border-bottom: 1px solid #1a202c"
        >
          <div style="flex: 1; min-width: 0">
            <div style="font-size: 13px; color: #a0aec0; font-family: monospace">
              {{ issue.currentSlug }} <span style="color: #4a5568">(id: {{ issue.currentId }})</span>
            </div>
          </div>
          <span style="color: #4a5568; font-size: 13px">&rarr;</span>
          <div style="flex: 1; min-width: 0">
            <div style="font-size: 13px; color: #6bcb6b; font-family: monospace">
              {{ issue.fixedSlug }} <span style="color: #4a5568">(id: {{ issue.fixedId }})</span>
            </div>
          </div>
        </div>
      </div>

      <div style="padding: 16px 24px; border-top: 1px solid #2d3748; display: flex; justify-content: flex-end; gap: 8px">
        <button
          style="font-size: 13px; color: #718096; background: none; border: 1px solid #2d3748; border-radius: 6px; padding: 6px 16px; cursor: pointer"
          @click="emit('close')"
        >Cancel</button>
        <button
          :disabled="fixing"
          style="font-size: 13px; color: #1a202c; background: #ed8936; border: none; border-radius: 6px; padding: 6px 16px; cursor: pointer; font-weight: 600"
          @click="fix"
        >{{ fixing ? 'Fixing...' : 'Fix all' }}</button>
      </div>
    </div>
  </div>
</template>
