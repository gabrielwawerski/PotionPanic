import {ref} from "vue";

export function useTicketWriter() {
  const error = ref<string | null>(null);
  const saving = ref(false);
  const isDev = import.meta.env.DEV;

  async function writeTicket(url: string, updates: Record<string, unknown>) {
    if (!isDev) {
      return;
    }

    saving.value = true;
    error.value = null;

    try {
      const response = await fetch("/__vitepress_pm_update", {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({url, updates}),
      });

      if (!response.ok) {
        error.value = await response.text();
      }
    } catch (cause) {
      error.value = String(cause);
    } finally {
      saving.value = false;
    }
  }

  return {error, saving, writeTicket};
}
