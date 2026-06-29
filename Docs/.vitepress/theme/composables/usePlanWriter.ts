import {ref} from "vue";

export interface PlanDocument {
  body: string;
  date: string;
  filePath: string;
  title: string;
  url: string;
}

export function usePlanWriter() {
  const error = ref<string | null>(null);
  const loading = ref(false);
  const saving = ref(false);
  const isDev = import.meta.env.DEV;

  async function requestJson(
    endpoint: string,
    options: RequestInit = {},
    state: "loading" | "saving" = "saving"
  ) {
    if (!isDev) {
      return null;
    }

    error.value = null;
    if (state === "loading") {
      loading.value = true;
    } else {
      saving.value = true;
    }

    try {
      const response = await fetch(endpoint, options);
      if (!response.ok) {
        error.value = await response.text();
        return null;
      }

      return await response.json();
    } catch (cause) {
      error.value = String(cause);
      return null;
    } finally {
      if (state === "loading") {
        loading.value = false;
      } else {
        saving.value = false;
      }
    }
  }

  async function loadPlan(url: string) {
    const params = new URLSearchParams({url});
    return await requestJson(
      `/__vitepress_pm_plan?${params.toString()}`,
      undefined,
      "loading"
    ) as PlanDocument | null;
  }

  async function createPlan(payload: {body: string; title: string}) {
    return await requestJson("/__vitepress_pm_create_plan", {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify(payload),
    }) as PlanDocument | null;
  }

  async function updatePlan(payload: {body: string; title: string; url: string}) {
    return await requestJson("/__vitepress_pm_update_plan", {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify(payload),
    }) as PlanDocument | null;
  }

  return {createPlan, error, loadPlan, loading, saving, updatePlan};
}
