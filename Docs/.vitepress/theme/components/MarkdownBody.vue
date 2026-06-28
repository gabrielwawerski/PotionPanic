<script setup lang="ts">
defineProps<{
  interactive?: boolean
  text: string
}>()

const emit = defineEmits<{
  checkboxToggle: [index: number]
}>()

interface ParsedLine {
  type: 'checkbox' | 'heading2' | 'heading3' | 'list' | 'empty' | 'code-fence' | 'paragraph'
  content: string
  checked?: boolean
  checkboxIndex?: number
  indent?: number
}

function parseLines(text: string): ParsedLine[] {
  const lines = text.split('\n')
  let checkboxIndex = -1

  return lines.map((line) => {
    const checkbox = line.match(/^(\s*)- \[([ x])\] (.+)$/)
    if (checkbox) {
      checkboxIndex += 1
      return {
        type: 'checkbox',
        content: checkbox[3],
        checked: checkbox[2] === 'x',
        checkboxIndex,
        indent: checkbox[1].length,
      }
    }

    if (line.startsWith('## ')) {
      return { type: 'heading2', content: line.slice(3) }
    }
    if (line.startsWith('### ')) {
      return { type: 'heading3', content: line.slice(4) }
    }
    if (line.startsWith('```')) {
      return { type: 'code-fence', content: '' }
    }
    if (line.startsWith('- ')) {
      return { type: 'list', content: line.slice(2) }
    }
    if (line.trim() === '') {
      return { type: 'empty', content: '' }
    }

    return { type: 'paragraph', content: line }
  })
}
</script>

<template>
  <div v-if="!text" style="color: #4a5568; font-size: 13px; font-style: italic">No content.</div>
  <div v-else>
    <template v-for="(line, index) in parseLines(text)" :key="index">
      <div
        v-if="line.type === 'checkbox'"
        style="display: flex; align-items: flex-start; gap: 8px; padding: 3px 0; text-align: left; width: 100%"
        :style="{
          paddingLeft: `${12 + (line.indent || 0) * 12}px`,
          cursor: interactive ? 'pointer' : 'default',
        }"
        @click="interactive && emit('checkboxToggle', line.checkboxIndex!)"
      >
        <div
          :style="{
            width: '15px',
            height: '15px',
            borderRadius: '3px',
            flexShrink: 0,
            marginTop: '1px',
            border: line.checked ? 'none' : '2px solid #4a5568',
            background: line.checked ? '#2d5a2d' : 'transparent',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '9px',
            color: '#6bcb6b',
          }"
        >
          <span v-if="line.checked">&#10003;</span>
        </div>
        <span
          :style="{
            fontSize: '13px',
            color: line.checked ? '#6bcb6b' : '#cbd5e0',
            opacity: line.checked ? 0.75 : 1,
            lineHeight: '1.4',
          }"
        >{{ line.content }}</span>
      </div>

      <h2
        v-else-if="line.type === 'heading2'"
        style="font-size: 18px; font-weight: 700; color: #e2e8f0; margin: 24px 0 8px 0; padding-bottom: 6px; border-bottom: 1px solid #2d3748"
      >{{ line.content }}</h2>

      <h3
        v-else-if="line.type === 'heading3'"
        style="font-size: 15px; font-weight: 600; color: #a0aec0; margin: 16px 0 6px 0"
      >{{ line.content }}</h3>

      <div
        v-else-if="line.type === 'list'"
        style="font-size: 13px; color: #cbd5e0; padding: 2px 0 2px 16px; line-height: 1.6"
      >&bull; {{ line.content }}</div>

      <div v-else-if="line.type === 'empty'" style="height: 8px" />
      <div v-else-if="line.type === 'code-fence'" style="height: 0" />

      <p
        v-else
        style="font-size: 14px; color: #cbd5e0; margin: 3px 0; line-height: 1.7"
      >{{ line.content }}</p>
    </template>
  </div>
</template>
