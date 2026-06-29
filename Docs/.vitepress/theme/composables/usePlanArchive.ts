import {ref} from "vue";

export interface ArchivedPlanResult {
  title: string;
  url: string;
}

export function usePlanArchive() {
  const error = ref<string | null>(null);
  const saving = ref(false);
  const isDev = import.meta.env.DEV;

  async function archivePlan(url: string) {
    if (!isDev) {
      return null;
    }

    saving.value = true;
    error.value = null;

    try {
      const response = await fetch("/__vitepress_pm_archive_plan", {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({url}),
      });

      if (!response.ok) {
        error.value = await response.text();
        return null;
      }

      return await response.json() as ArchivedPlanResult;
    } catch (cause) {
      error.value = String(cause);
      return null;
    } finally {
      saving.value = false;
    }
  }

  return {archivePlan, error, saving};
}
