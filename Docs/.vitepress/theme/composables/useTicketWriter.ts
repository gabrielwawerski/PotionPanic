import {ref} from "vue";

export function useTicketWriter() {
  const error = ref<string | null>(null);
  const saving = ref(false);
  const isDev = import.meta.env.DEV;

  async function postJson(endpoint: string, payload: Record<string, unknown>) {
    if (!isDev) {
      return null;
    }

    saving.value = true;
    error.value = null;

    try {
      const response = await fetch(endpoint, {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        error.value = await response.text();
        return null;
      }

      const contentType = response.headers.get("content-type") || "";
      if (contentType.includes("application/json")) {
        return await response.json();
      }

      return await response.text();
    } catch (cause) {
      error.value = String(cause);
      return null;
    } finally {
      saving.value = false;
    }
  }

  async function writeTicket(url: string, updates: Record<string, unknown>) {
    await postJson("/__vitepress_pm_update", {url, updates});
  }

  async function writeTickets(updates: Array<{url: string; updates: Record<string, unknown>}>) {
    await postJson("/__vitepress_pm_update_batch", {updates});
  }

  async function archiveTicket(url: string, targetDir: string) {
    return await postJson("/__vitepress_pm_archive", {targetDir, url});
  }

  async function restoreTicket(url: string, targetDir: string) {
    return await postJson("/__vitepress_pm_restore", {targetDir, url});
  }

  return {
    archiveTicket,
    error,
    restoreTicket,
    saving,
    writeTicket,
    writeTickets,
  };
}
