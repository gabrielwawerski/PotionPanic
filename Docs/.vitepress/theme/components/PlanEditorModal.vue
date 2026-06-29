<script setup lang="ts">
import {computed, nextTick, ref, watch} from "vue";

import type {PlanDocument} from "../composables/usePlanWriter";

const props = defineProps<{
  error?: string | null
  mode: "create" | "edit"
  plan: PlanDocument
  saving?: boolean
}>();

const emit = defineEmits<{
  close: []
  save: [plan: PlanDocument]
}>();

const titleRef = ref<HTMLInputElement | null>(null);
const draft = ref<PlanDocument>({...props.plan});
const filePreview = computed(() => {
  if (props.mode === "edit") {
    return draft.value.filePath;
  }

  const slug = `${draft.value.title ?? ""}`
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "") || "plan";

  return `plans/${slug}.md`;
});

watch(() => props.plan, (value) => {
  draft.value = {...value};
}, {deep: true, immediate: true});

watch(() => props.mode, async () => {
  await nextTick();
  titleRef.value?.focus();
}, {immediate: true});

function onSave() {
  emit("save", {
    ...draft.value,
    body: `${draft.value.body ?? ""}`.trim(),
    title: `${draft.value.title ?? ""}`.trim(),
  });
}
</script>

<template>
  <div
    style="position: fixed; inset: 0; z-index: 100; display: flex; align-items: center; justify-content: center; background: rgba(0, 0, 0, 0.6); backdrop-filter: blur(2px)"
    @click.self="emit('close')"
  >
    <div
      style="width: min(960px, calc(100vw - 20px)); height: min(88vh, 900px); background: #0d1117; border: 1px solid #2d3748; border-radius: 12px; display: flex; flex-direction: column; overflow: hidden; box-shadow: 0 24px 48px rgba(0,0,0,0.4)"
    >
      <div style="display: flex; align-items: center; gap: 12px; padding: 16px 20px; border-bottom: 1px solid #2d3748; background: #171923">
        <strong style="font-size: 16px; color: #e2e8f0">{{ mode === "create" ? "New Plan" : "Edit Plan" }}</strong>
        <button
          style="margin-left: auto; background: none; border: none; color: #718096; cursor: pointer; font-size: 20px; line-height: 1"
          @click="emit('close')"
        >&times;</button>
      </div>

      <div style="display: flex; flex-direction: column; gap: 16px; padding: 16px 20px; overflow: auto">
        <label style="display: flex; flex-direction: column; gap: 6px">
          <span style="font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700">Title</span>
          <input
            ref="titleRef"
            v-model="draft.title"
            style="width: 100%; padding: 10px 12px; background: #111827; color: #e2e8f0; border: 1px solid #2d3748; border-radius: 8px; outline: none"
          >
        </label>

        <label style="display: flex; flex-direction: column; gap: 6px">
          <span style="font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700">File</span>
          <input
            :value="filePreview"
            readonly
            style="width: 100%; padding: 10px 12px; background: #0b1220; color: #94a3b8; border: 1px solid #1f2937; border-radius: 8px; outline: none; font-family: monospace"
          >
        </label>

        <label style="display: flex; flex-direction: column; gap: 6px; flex: 1">
          <span style="font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 1px; font-weight: 700">Body</span>
          <textarea
            v-model="draft.body"
            style="width: 100%; min-height: 420px; padding: 12px; background: #111827; color: #e2e8f0; border: 1px solid #2d3748; border-radius: 8px; outline: none; resize: vertical; font-family: 'JetBrains Mono', monospace; line-height: 1.6; box-sizing: border-box"
          />
        </label>

        <p v-if="error" style="margin: 0; color: #f87171; font-size: 12px">{{ error }}</p>
      </div>

      <div style="display: flex; justify-content: flex-end; gap: 8px; padding: 16px 20px; border-top: 1px solid #2d3748; background: #171923">
        <button
          style="padding: 8px 14px; background: none; color: #cbd5e0; border: 1px solid #2d3748; border-radius: 999px; cursor: pointer"
          @click="emit('close')"
        >Cancel</button>
        <button
          :disabled="saving || !draft.title.trim()"
          style="padding: 8px 14px; background: rgba(99, 179, 237, 0.16); color: #90cdf4; border: 1px solid rgba(99, 179, 237, 0.4); border-radius: 999px; cursor: pointer; font-weight: 700"
          @click="onSave"
        >{{ saving ? "Saving..." : "Save" }}</button>
      </div>
    </div>
  </div>
</template>
