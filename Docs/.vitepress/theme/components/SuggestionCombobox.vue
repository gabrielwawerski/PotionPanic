<script setup lang="ts">
import {computed, ref, watch} from "vue";

import type {SuggestionOption} from "../types";

const props = withDefaults(defineProps<{
  modelValue: string
  options: SuggestionOption[]
  placeholder?: string
}>(), {
  placeholder: "",
})

const emit = defineEmits<{
  "update:modelValue": [value: string]
}>()

const activeIndex = ref(0)
const inputRef = ref<HTMLInputElement | null>(null)
const isFocused = ref(false)
const inputValue = ref(props.modelValue || "")
const pendingBlur = ref<number | null>(null)

const filteredOptions = computed(() => {
  const normalizedQuery = inputValue.value.trim().toLowerCase()

  return props.options.filter((option) => {
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

watch(() => props.modelValue, (value) => {
  if (!isFocused.value) {
    inputValue.value = value || ""
  }
})

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

function commitValue(value: string) {
  const normalizedValue = `${value ?? ""}`.trim()
  emit("update:modelValue", normalizedValue)
  inputValue.value = normalizedValue
}

function selectOption(value: string) {
  cancelPendingBlur()
  commitValue(value)
  isFocused.value = false
  inputRef.value?.blur()
}

function onFocus() {
  cancelPendingBlur()
  isFocused.value = true
}

function onBlur() {
  if (typeof window === "undefined") {
    isFocused.value = false
    commitValue(inputValue.value)
    return
  }

  pendingBlur.value = window.setTimeout(() => {
    isFocused.value = false
    commitValue(inputValue.value)
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

    commitValue(inputValue.value)
    inputRef.value?.blur()
    return
  }

  if (event.key === "Escape") {
    inputValue.value = props.modelValue || ""
    isFocused.value = false
    inputRef.value?.blur()
  }
}
</script>

<template>
  <div style="position: relative">
    <input
      ref="inputRef"
      v-model="inputValue"
      :placeholder="placeholder"
      style="width: 100%; font-size: 12px; padding: 7px 10px; background: #0d1117; border: 1px solid #2d3748; border-radius: 6px; color: #e2e8f0; outline: none; box-sizing: border-box"
      @blur="onBlur"
      @focus="onFocus"
      @keydown="onKeydown"
    >

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
          background: index === activeIndex ? 'rgba(230, 168, 23, 0.14)' : 'transparent',
          color: index === activeIndex ? '#f7fafc' : '#cbd5e0',
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
