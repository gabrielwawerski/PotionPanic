<script setup lang="ts">
import {computed, onBeforeUnmount, onMounted, ref, watch} from 'vue'
import { useData } from 'vitepress'
import DefaultTheme from 'vitepress/theme'

import Board from './components/Board.vue'
import PlanAuthoringControls from './components/PlanAuthoringControls.vue'
import {
  buildBoardPageKey,
  buildBoardShellClasses,
  readBoardSidebarCollapsed,
  writeBoardSidebarCollapsed
} from '../lib/board-shell.mjs'
import {buildProjectRootPagePath} from '../lib/page-path-copy.mjs'

const { frontmatter, page } = useData()

const isBoardPage = computed(() => !!frontmatter.value.board)
const boardSidebarCollapsed = ref(false)
const pagePathCopied = ref(false)
let pagePathCopiedResetId: ReturnType<typeof window.setTimeout> | null = null

const projectRootPagePath = computed(() => (
  buildProjectRootPagePath(page.value.relativePath)
))
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
const showPagePathCopyButton = computed(() => (
  !isBoardPage.value && !!projectRootPagePath.value
))
const pagePathCopyLabel = computed(() => (
  pagePathCopied.value ? 'Copied' : 'Copy Path'
))

function syncBoardSidebarPreference() {
  if (typeof window === 'undefined') {
    return
  }

  boardSidebarCollapsed.value = readBoardSidebarCollapsed(window.localStorage)
}

function toggleBoardSidebar() {
  boardSidebarCollapsed.value = !boardSidebarCollapsed.value
}

function clearPagePathCopiedReset() {
  if (pagePathCopiedResetId === null) {
    return
  }

  clearTimeout(pagePathCopiedResetId)
  pagePathCopiedResetId = null
}

async function copyPagePath() {
  if (
    !projectRootPagePath.value
    || typeof navigator === 'undefined'
    || !navigator.clipboard?.writeText
  ) {
    return
  }

  await navigator.clipboard.writeText(projectRootPagePath.value)
  pagePathCopied.value = true
  clearPagePathCopiedReset()
  pagePathCopiedResetId = window.setTimeout(() => {
    pagePathCopied.value = false
    pagePathCopiedResetId = null
  }, 1800)
}

onMounted(() => {
  if (isBoardPage.value) {
    syncBoardSidebarPreference()
  }
})

onBeforeUnmount(() => {
  clearPagePathCopiedReset()
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

      <template #doc-before>
        <PlanAuthoringControls />
        <div v-if="showPagePathCopyButton" class="doc-page-actions">
          <button
            class="doc-page-copy-path-button"
            type="button"
            :title="projectRootPagePath ?? undefined"
            @click="copyPagePath"
          >
            {{ pagePathCopyLabel }}
          </button>
        </div>
      </template>
    </DefaultTheme.Layout>
  </div>
</template>

<style scoped>
.doc-page-actions {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 12px;
}

.doc-page-copy-path-button {
  padding: 7px 12px;
  border: 1px solid rgba(100, 116, 139, 0.35);
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.04);
  color: var(--vp-c-text-2);
  font-size: 12px;
  font-weight: 600;
  line-height: 1;
  cursor: pointer;
  transition: border-color 0.2s ease, background-color 0.2s ease,
    color 0.2s ease;
}

.doc-page-copy-path-button:hover {
  border-color: var(--vp-c-brand-1);
  background: rgba(59, 130, 246, 0.08);
  color: var(--vp-c-text-1);
}

.doc-page-copy-path-button:focus-visible {
  outline: 2px solid var(--vp-c-brand-1);
  outline-offset: 2px;
}
</style>
