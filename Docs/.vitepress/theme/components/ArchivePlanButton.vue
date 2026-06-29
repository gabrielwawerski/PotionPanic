<script setup lang="ts">
import {computed} from "vue";
import {useData, useRouter} from "vitepress";

import {
  buildPlanPageUrl,
  isArchivablePlanPage,
} from "../../lib/plan-archive-page.mjs";
import {usePlanArchive} from "../composables/usePlanArchive";

const {page} = useData();
const router = useRouter();
const {archivePlan, error, saving} = usePlanArchive();

const archiveUrl = computed(() => buildPlanPageUrl(page.value.relativePath));
const canArchive = computed(() => (
  import.meta.env.DEV && isArchivablePlanPage(page.value.relativePath)
));

async function onArchivePlan() {
  if (!canArchive.value) {
    return;
  }

  const confirmed = window.confirm(
    "Archive this plan? It will move from Active Plans into Archived Plans."
  );
  if (!confirmed) {
    return;
  }

  const archived = await archivePlan(archiveUrl.value);
  if (archived?.url) {
    await router.go(archived.url);
  }
}
</script>

<template>
  <div v-if="canArchive" class="archive-plan-callout">
    <div class="archive-plan-copy">
      <strong>Archive this plan</strong>
      <span>
        Move completed or superseded work out of Active Plans and into the
        Archived Plans section.
      </span>
    </div>

    <button
      class="archive-plan-button"
      type="button"
      :disabled="saving"
      @click="onArchivePlan"
    >
      {{ saving ? "Archiving..." : "Archive Plan" }}
    </button>

    <p v-if="error" class="archive-plan-error">{{ error }}</p>
  </div>
</template>

<style scoped>
.archive-plan-callout {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  margin: 24px 0;
  padding: 14px 16px;
  border: 1px solid rgba(237, 137, 54, 0.35);
  border-radius: 12px;
  background: rgba(237, 137, 54, 0.08);
}

.archive-plan-copy {
  display: flex;
  flex: 1 1 280px;
  flex-direction: column;
  gap: 4px;
  color: var(--vp-c-text-1);
  font-size: 13px;
  line-height: 1.5;
}

.archive-plan-button {
  padding: 8px 12px;
  border: 1px solid rgba(237, 137, 54, 0.55);
  border-radius: 999px;
  background: rgba(237, 137, 54, 0.16);
  color: #ed8936;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
}

.archive-plan-button:disabled {
  opacity: 0.7;
  cursor: progress;
}

.archive-plan-error {
  flex-basis: 100%;
  margin: 0;
  color: #f56565;
  font-size: 12px;
}
</style>
