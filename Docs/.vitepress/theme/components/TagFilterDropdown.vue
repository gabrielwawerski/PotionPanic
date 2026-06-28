<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

const props = defineProps<{
  modelValue: string[]
  tags: string[]
}>()

const emit = defineEmits<{
  'update:modelValue': [tags: string[]]
}>()

const dropdownRef = ref<HTMLElement | null>(null)
const open = ref(false)
const search = ref('')

const filteredTags = computed(() => {
  if (!search.value) {
    return props.tags
  }

  const query = search.value.toLowerCase()
  return props.tags.filter((tag) => tag.toLowerCase().includes(query))
})

function clear() {
  emit('update:modelValue', [])
}

function onClickOutside(event: MouseEvent) {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    open.value = false
  }
}

function toggle(tag: string) {
  const selected = new Set(props.modelValue)
  if (selected.has(tag)) {
    selected.delete(tag)
  } else {
    selected.add(tag)
  }

  emit('update:modelValue', [...selected])
}

onMounted(() => document.addEventListener('mousedown', onClickOutside))
onBeforeUnmount(() => document.removeEventListener('mousedown', onClickOutside))
</script>

<template>
  <div ref="dropdownRef" style="position: relative">
    <button
      style="font-size: 12px; padding: 4px 12px; background: #2d3748; border: 1px solid #4a5568; border-radius: 5px; color: #e2e8f0; cursor: pointer; font-weight: 600; line-height: 1.2; height: 28px; box-sizing: border-box"
      @click="open = !open"
    >
      Tags<span v-if="modelValue.length > 0"> ({{ modelValue.length }})</span>
    </button>

    <div
      v-if="open"
      style="position: absolute; top: calc(100% + 4px); left: 0; z-index: 100; min-width: 220px; background: #171923; border: 1px solid #2d3748; border-radius: 6px; box-shadow: 0 4px 12px rgba(0,0,0,0.4); display: flex; flex-direction: column; max-height: 300px"
    >
      <div style="padding: 8px; border-bottom: 1px solid #2d3748; display: flex; align-items: center; gap: 6px">
        <input
          v-model="search"
          placeholder="Search tags..."
          style="flex: 1; font-size: 12px; padding: 4px 8px; background: #0d1117; border: 1px solid #2d3748; border-radius: 4px; color: #e2e8f0; outline: none"
        >
        <button
          v-if="modelValue.length > 0"
          style="font-size: 11px; background: none; border: none; color: #718096; cursor: pointer; white-space: nowrap"
          @click="clear"
        >clear</button>
      </div>

      <div style="overflow-y: auto; padding: 4px 0">
        <label
          v-for="tag in filteredTags"
          :key="tag"
          style="display: flex; align-items: center; gap: 8px; padding: 4px 12px; cursor: pointer; font-size: 12px; color: #e2e8f0"
          @mouseenter="($event.currentTarget as HTMLElement).style.background = '#2d3748'"
          @mouseleave="($event.currentTarget as HTMLElement).style.background = 'transparent'"
        >
          <input
            type="checkbox"
            :checked="modelValue.includes(tag)"
            style="accent-color: #63b3ed"
            @change="toggle(tag)"
          >
          <span style="display: inline-block; font-size: 11px; padding: 1px 8px; border-radius: 10px; background: #2d3748; color: #a0aec0">{{ tag }}</span>
        </label>
        <div
          v-if="filteredTags.length === 0"
          style="padding: 8px 12px; font-size: 12px; color: #718096"
        >No tags found</div>
      </div>
    </div>
  </div>
</template>
