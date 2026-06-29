import {ref} from "vue";

export function useDragDrop(
  onDrop: (
    ticketId: string,
    targetColumn: string,
    targetIndex: number
  ) => void
) {
  const dragOverColumn = ref<string | null>(null);
  const dragOverIndex = ref<number | null>(null);

  function handleDragStart(event: DragEvent, ticketId: string) {
    if (!event.dataTransfer) {
      return;
    }

    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", ticketId);
    requestAnimationFrame(() => {
      const element = event.target as HTMLElement;
      element.style.opacity = "0.4";
    });
  }

  function handleDragEnd(event: DragEvent) {
    const element = event.target as HTMLElement;
    element.style.opacity = "1";
    dragOverColumn.value = null;
    dragOverIndex.value = null;
  }

  function handleDragOver(event: DragEvent, columnKey: string, index: number) {
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = "move";
    }
    dragOverColumn.value = columnKey;
    dragOverIndex.value = index;
  }

  function handleDragLeave() {
    dragOverColumn.value = null;
    dragOverIndex.value = null;
  }

  function handleDrop(event: DragEvent, columnKey: string, index: number) {
    event.preventDefault();
    const ticketId = event.dataTransfer?.getData("text/plain");
    if (ticketId) {
      onDrop(ticketId, columnKey, index);
    }
    dragOverColumn.value = null;
    dragOverIndex.value = null;
  }

  return {
    dragOverColumn,
    dragOverIndex,
    handleDragEnd,
    handleDragLeave,
    handleDragOver,
    handleDragStart,
    handleDrop,
  };
}
