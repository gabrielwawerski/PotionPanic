<script setup lang="ts">
import {computed, onMounted, ref, watch} from 'vue'
import { useData } from 'vitepress'
import DefaultTheme from 'vitepress/theme'

import Board from './components/Board.vue'
import {
  buildBoardPageKey,
  buildBoardShellClasses,
  readBoardSidebarCollapsed,
  writeBoardSidebarCollapsed
} from '../lib/board-shell.mjs'

const { frontmatter, page } = useData()

const isBoardPage = computed(() => !!frontmatter.value.board)
const boardSidebarCollapsed = ref(false)
const boardComponentKey = computed(() => buildBoardPageKey({
  board: isBoardPage.value,
  relativePath: page.value.relativePath,
  ticketsDir: frontmatter.value.ticketsDir,
}))

const boardSidebarToggleLabel = computed(() => (
  boardSidebarCollapsed.value ? 'Show board nav' : 'Hide board nav'
))

const layoutClasses = computed(() => buildBoardShellClasses({
  board: isBoardPage.value,
  collapsed: boardSidebarCollapsed.value,
}))

function syncBoardSidebarPreference() {
  if (typeof window === 'undefined') {
    return
  }

  boardSidebarCollapsed.value = readBoardSidebarCollapsed(window.localStorage)
}

function toggleBoardSidebar() {
  boardSidebarCollapsed.value = !boardSidebarCollapsed.value
}

onMounted(() => {
  if (isBoardPage.value) {
    syncBoardSidebarPreference()
  }
})

watch(isBoardPage, (next) => {
  if (next) {
    syncBoardSidebarPreference()
  }
})

watch(boardSidebarCollapsed, (collapsed) => {
  if (!isBoardPage.value || typeof window === 'undefined') {
    return
  }

  writeBoardSidebarCollapsed(window.localStorage, collapsed)
})
</script>

<template>
  <div :class="layoutClasses">
    <DefaultTheme.Layout>
      <template #nav-bar-content-before>
        <button
          v-if="isBoardPage"
          :aria-controls="'VPSidebarNav'"
          :aria-expanded="boardSidebarCollapsed ? 'false' : 'true'"
          :aria-label="boardSidebarToggleLabel"
          class="board-shell-toggle"
          type="button"
          @click="toggleBoardSidebar"
        >
          <span aria-hidden="true" class="board-shell-toggle-icon">
            {{ boardSidebarCollapsed ? '>' : '<' }}
          </span>
          <span class="board-shell-toggle-label">{{ boardSidebarToggleLabel }}</span>
        </button>
      </template>

      <template #page-top>
        <Board v-if="frontmatter.board" :key="boardComponentKey" />
      </template>
    </DefaultTheme.Layout>
  </div>
</template>
