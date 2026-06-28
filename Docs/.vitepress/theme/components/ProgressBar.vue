<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  done: number
  total: number
  color?: string
}>()

const percentage = computed(() => (
  props.total === 0 ? 0 : Math.round((props.done / props.total) * 100)
))
</script>

<template>
  <div style="display: flex; align-items: center; gap: 6px">
    <div style="flex: 1; height: 4px; background: var(--board-border, #2d3748); border-radius: 2px; overflow: hidden">
      <div
        :style="{
          width: `${percentage}%`,
          height: '100%',
          background: color || '#718096',
          borderRadius: '2px',
          transition: 'width 0.3s',
        }"
      />
    </div>
    <span style="font-size: 10px; color: var(--board-text-muted, #718096)">{{ done }}/{{ total }}</span>
  </div>
</template>
