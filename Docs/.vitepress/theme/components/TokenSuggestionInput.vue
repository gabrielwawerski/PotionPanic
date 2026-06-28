<script setup lang="ts">
import {computed, ref, watch} from "vue";

import type {SuggestionOption} from "../types";

const props = withDefaults(defineProps<{
  modelValue: string[]
  normalizeValue?: (value: string) => string
  options: SuggestionOption[]
  placeholder?: string
}>(), {
  normalizeValue: (value: string) => `${value ?? ""}`.trim(),
  placeholder: "",
})

const emit = defineEmits<{
  "update:modelValue": [value: string[]]
}>()

const activeIndex = ref(0)
const inputRef = ref<HTMLInputElement | null>(null)
const isFocused = ref(false)
const pendingBlur = ref<number | null>(null)
const query = ref("")

const optionMap = computed(() => new Map(
  props.options.map((option) => [option.value, option])
))
const selectedSet = computed(() => new Set(props.modelValue))

const filteredOptions = computed(() => {
  const normalizedQuery = query.value.trim().toLowerCase()

  return props.options.filter((option) => {
    if (selectedSet.value.has(option.value)) {
      return false
    }

    if (!normalizedQuery) {
      return true
    }

    return option.value.toLowerCase().includes(normalizedQuery)
      || option.label.toLowerCase().includes(normalizedQuery)
  }).slice(0, 8)
})

const showSuggestions = computed(() => (
  isFocused.value && filteredOptions.value.length > 0
))

watch(filteredOptions, (options) => {
  if (options.length === 0) {
    activeIndex.value = 0
    return
  }

  if (activeIndex.value >= options.length) {
    activeIndex.value = options.length - 1
  }
})

function cancelPendingBlur() {
  if (pendingBlur.value !== null && typeof window !== "undefined") {
    window.clearTimeout(pendingBlur.value)
    pendingBlur.value = null
  }
}

function focusInput() {
  inputRef.value?.focus()
}

function displayLabel(value: string) {
  return optionMap.value.get(value)?.label || value
}

function normalizeCandidate(value: string) {
  return props.normalizeValue(`${value ?? ""}`)
}

function addValue(value: string) {
  const normalizedValue = normalizeCandidate(value)
  if (!normalizedValue || selectedSet.value.has(normalizedValue)) {
    query.value = ""
    return
  }

  emit("update:modelValue", [...props.modelValue, normalizedValue])
  query.value = ""
  activeIndex.value = 0
}

function removeValue(value: string) {
  emit("update:modelValue", props.modelValue.filter((entry) => entry !== value))
}

function commitCustomValue() {
  if (!query.value.trim()) {
    query.value = ""
    return
  }

  addValue(query.value)
}

function selectOption(value: string) {
  cancelPendingBlur()
  addValue(value)
  focusInput()
}

function onFocus() {
  cancelPendingBlur()
  isFocused.value = true
}

function onBlur() {
  if (typeof window === "undefined") {
    isFocused.value = false
    commitCustomValue()
    return
  }

  pendingBlur.value = window.setTimeout(() => {
    isFocused.value = false
    commitCustomValue()
    activeIndex.value = 0
    pendingBlur.value = null
  }, 120)
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === "ArrowDown") {
    if (filteredOptions.value.length === 0) {
      return
    }

    event.preventDefault()
    activeIndex.value = (activeIndex.value + 1) % filteredOptions.value.length
    return
  }

  if (event.key === "ArrowUp") {
    if (filteredOptions.value.length === 0) {
      return
    }

    event.preventDefault()
    activeIndex.value = (
      activeIndex.value - 1 + filteredOptions.value.length
    ) % filteredOptions.value.length
    return
  }

  if (event.key === "Enter") {
    event.preventDefault()
    if (showSuggestions.value) {
      selectOption(filteredOptions.value[activeIndex.value].value)
      return
    }

    commitCustomValue()
    return
  }

  if (event.key === "Escape") {
    query.value = ""
    isFocused.value = false
    inputRef.value?.blur()
    return
  }

  if (event.key === "Backspace" && query.value === "" && props.modelValue.length > 0) {
    removeValue(props.modelValue[props.modelValue.length - 1])
  }
}
</script>

<template>
  <div style="position: relative">
    <div
      style="display: flex; flex-wrap: wrap; align-items: center; gap: 4px; padding: 4px 8px; background: #0d1117; border: 1px solid #2d3748; border-radius: 6px; min-height: 32px"
      @click="focusInput"
    >
      <span
        v-for="value in modelValue"
        :key="value"
        style="display: inline-flex; align-items: center; gap: 4px; font-size: 11px; padding: 2px 8px; border-radius: 999px; background: rgba(99, 179, 237, 0.12); border: 1px solid rgba(99, 179, 237, 0.28); color: #bee3f8; white-space: nowrap"
      >
        <span>{{ displayLabel(value) }}</span>
        <button
          style="background: none; border: none; color: #90cdf4; cursor: pointer; font-size: 12px; padding: 0; line-height: 1; display: flex; align-items: center"
          @click.stop="removeValue(value)"
        >&times;</button>
      </span>

      <input
        ref="inputRef"
        v-model="query"
        :placeholder="placeholder"
        style="flex: 1; min-width: 96px; font-size: 12px; padding: 2px 0; background: transparent; border: none; color: #e2e8f0; outline: none"
        @blur="onBlur"
        @focus="onFocus"
        @keydown="onKeydown"
      >
    </div>

    <div
      v-if="showSuggestions"
      style="position: absolute; left: 0; right: 0; top: calc(100% + 6px); z-index: 20; background: #111827; border: 1px solid #2d3748; border-radius: 8px; box-shadow: 0 14px 30px rgba(0, 0, 0, 0.35); overflow: hidden"
    >
      <button
        v-for="(option, index) in filteredOptions"
        :key="option.value"
        type="button"
        :style="{
          width: '100%',
          textAlign: 'left',
          padding: '8px 10px',
          border: 'none',
          borderTop: index === 0 ? 'none' : '1px solid rgba(45, 55, 72, 0.7)',
          background: index === activeIndex ? 'rgba(99, 179, 237, 0.14)' : 'transparent',
          color: index === activeIndex ? '#e2e8f0' : '#cbd5e0',
          cursor: 'pointer',
        }"
        @mousedown.prevent="selectOption(option.value)"
        @mouseenter="activeIndex = index"
      >
        <span style="display: block; font-size: 12px; line-height: 1.4">{{ option.label }}</span>
      </button>
    </div>
  </div>
</template>
