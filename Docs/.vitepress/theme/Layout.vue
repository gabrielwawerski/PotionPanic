<script setup lang="ts">
import { useData } from 'vitepress'
import DefaultTheme from 'vitepress/theme'

import Board from './components/Board.vue'

const { frontmatter } = useData()
</script>

<template>
  <DefaultTheme.Layout>
    <template #page-top>
      <Board v-if="frontmatter.board" />
    </template>
  </DefaultTheme.Layout>
</template>
