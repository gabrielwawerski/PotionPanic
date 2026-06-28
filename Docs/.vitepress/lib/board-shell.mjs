export const BOARD_SIDEBAR_COLLAPSED_STORAGE_KEY =
  "potion-panic.boardSidebarCollapsed";

export function readBoardSidebarCollapsed(storage) {
  if (!storage || typeof storage.getItem !== "function") {
    return false;
  }

  return storage.getItem(BOARD_SIDEBAR_COLLAPSED_STORAGE_KEY) === "true";
}

export function writeBoardSidebarCollapsed(storage, collapsed) {
  if (!storage || typeof storage.setItem !== "function") {
    return;
  }

  storage.setItem(BOARD_SIDEBAR_COLLAPSED_STORAGE_KEY,
    collapsed ? "true" : "false");
}

export function buildBoardShellClasses({ board, collapsed }) {
  return {
    "board-shell-layout": !!board,
    "board-sidebar-collapsed": !!board && !!collapsed,
  };
}
