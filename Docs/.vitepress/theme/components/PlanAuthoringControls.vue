<script setup lang="ts">
import {computed, ref} from "vue";
import {useData, useRouter} from "vitepress";

import {DEFAULT_PLAN_TEMPLATE} from "../../lib/plan-common.mjs";
import {
  buildPlanPageUrl,
  isArchivablePlanPage,
} from "../../lib/plan-archive-page.mjs";
import {usePlanArchive} from "../composables/usePlanArchive";
import type {PlanDocument} from "../composables/usePlanWriter";
import {usePlanWriter} from "../composables/usePlanWriter";

import PlanEditorModal from "./PlanEditorModal.vue";

const {page} = useData();
const router = useRouter();
const {archivePlan, error: archiveError, saving: archiveSaving} = usePlanArchive();
const {
  createPlan,
  error: writerError,
  loadPlan,
  loading,
  saving,
  updatePlan,
} = usePlanWriter();

const editorMode = ref<"create" | "edit" | null>(null);
const editorPlan = ref<PlanDocument | null>(null);

const pageUrl = computed(() => buildPlanPageUrl(page.value.relativePath));
const canCreate = computed(() => (
  import.meta.env.DEV && page.value.relativePath === "plans/index.md"
));
const canEdit = computed(() => (
  import.meta.env.DEV && isArchivablePlanPage(page.value.relativePath)
));
const canArchive = computed(() => canEdit.value);
const canShowCallout = computed(() => canCreate.value || canEdit.value);
const errorMessage = computed(() => writerError.value || archiveError.value);

function openCreateModal() {
  editorMode.value = "create";
  editorPlan.value = {
    body: DEFAULT_PLAN_TEMPLATE,
    date: "",
    filePath: "",
    title: "New Plan",
    url: "",
  };
}

async function openEditModal() {
  if (!canEdit.value) {
    return;
  }

  const loaded = await loadPlan(pageUrl.value);
  if (!loaded) {
    return;
  }

  editorMode.value = "edit";
  editorPlan.value = loaded;
}

function closeModal() {
  editorMode.value = null;
  editorPlan.value = null;
}

async function savePlan(plan: PlanDocument) {
  if (!editorMode.value) {
    return;
  }

  if (editorMode.value === "create") {
    const created = await createPlan({
      body: plan.body,
      title: plan.title,
    });
    if (created?.url) {
      closeModal();
      await router.go(created.url);
    }
    return;
  }

  const updated = await updatePlan({
    body: plan.body,
    title: plan.title,
    url: pageUrl.value,
  });
  if (!updated?.url) {
    return;
  }

  closeModal();
  if (typeof window !== "undefined" && window.location.pathname === updated.url) {
    window.location.reload();
    return;
  }

  await router.go(updated.url);
}

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

  const archived = await archivePlan(pageUrl.value);
  if (archived?.url) {
    await router.go(archived.url);
  }
}
</script>

<template>
  <div v-if="canShowCallout" class="archive-plan-callout">
    <div class="archive-plan-copy">
      <strong>{{ canCreate ? "Create a new plan" : "Plan authoring" }}</strong>
      <span v-if="canCreate">
        Add a new active implementation plan directly from the docs site.
      </span>
      <span v-else>
        Edit this plan on-page or archive it when the work is complete.
      </span>
    </div>

    <div style="display: flex; flex-wrap: wrap; gap: 8px">
      <button
        v-if="canCreate"
        class="archive-plan-button"
        type="button"
        :disabled="loading || saving"
        @click="openCreateModal"
      >New Plan</button>

      <button
        v-if="canEdit"
        class="archive-plan-button"
        type="button"
        :disabled="loading || saving"
        @click="openEditModal"
      >Edit Plan</button>

      <button
        v-if="canArchive"
        class="archive-plan-button"
        type="button"
        :disabled="archiveSaving"
        @click="onArchivePlan"
      >{{ archiveSaving ? "Archiving..." : "Archive Plan" }}</button>
    </div>

    <p v-if="errorMessage" class="archive-plan-error">{{ errorMessage }}</p>
  </div>

  <PlanEditorModal
    v-if="editorMode && editorPlan"
    :error="writerError"
    :mode="editorMode"
    :plan="editorPlan"
    :saving="saving"
    @close="closeModal"
    @save="savePlan"
  />
</template>

<style scoped>
.archive-plan-callout {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  margin: 24px 0;
  padding: 14px 16px;
  border: 1px solid rgba(99, 179, 237, 0.35);
  border-radius: 12px;
  background: rgba(59, 130, 246, 0.08);
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
  border: 1px solid rgba(99, 179, 237, 0.55);
  border-radius: 999px;
  background: rgba(59, 130, 246, 0.16);
  color: #90cdf4;
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
