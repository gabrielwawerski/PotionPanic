<script setup lang="ts">
import { ref } from 'vue'

const props = withDefaults(defineProps<{
  readOnly?: boolean
  tags: string[]
}>(), {
  readOnly: false,
})

const emit = defineEmits<{
  add: [tag: string]
  remove: [tag: string]
}>()

const inputRef = ref<HTMLInputElement | null>(null)
const tagInput = ref('')

function addTag() {
  if (props.readOnly) {
    return
  }

  const tag = tagInput.value.trim().toLowerCase()
  if (tag) {
    emit('add', tag)
    tagInput.value = ''
  }
}

function focusInput() {
  if (!props.readOnly) {
    inputRef.value?.focus()
  }
}

function onKeydown(event: KeyboardEvent) {
  if (props.readOnly) {
    return
  }

  if (event.key === 'Enter') {
    event.preventDefault()
    addTag()
  }

  if (event.key === 'Backspace' && tagInput.value === '') {
    emit('remove', '')
  }
}
</script>

<template>
  <div
    style="display: flex; flex-wrap: wrap; align-items: center; gap: 4px; padding: 4px 8px; background: #0d1117; border: 1px solid #2d3748; border-radius: 6px; min-height: 32px"
    :style="{ cursor: readOnly ? 'default' : 'text' }"
    @click="focusInput"
  >
    <span
      v-for="tag in tags"
      :key="tag"
      style="display: inline-flex; align-items: center; gap: 3px; font-size: 11px; padding: 2px 8px; border-radius: 10px; background: #2d3748; color: #a0aec0; white-space: nowrap"
    >
      {{ tag }}
      <button
        v-if="!readOnly"
        style="background: none; border: none; color: #718096; cursor: pointer; font-size: 12px; padding: 0; line-height: 1; display: flex; align-items: center"
        @click.stop="emit('remove', tag)"
      >&times;</button>
    </span>
    <input
      v-if="!readOnly"
      ref="inputRef"
      v-model="tagInput"
      placeholder="Add tag..."
      style="flex: 1; min-width: 60px; font-size: 12px; padding: 2px 0; background: transparent; border: none; color: #e2e8f0; outline: none"
      @blur="addTag"
      @keydown="onKeydown"
    >
    <span v-else-if="tags.length === 0" style="font-size: 12px; color: #718096">No tags</span>
  </div>
</template>
