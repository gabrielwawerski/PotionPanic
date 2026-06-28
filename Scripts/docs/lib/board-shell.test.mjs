import assert from "node:assert/strict";
import test from "node:test";

import {
  BOARD_SIDEBAR_COLLAPSED_STORAGE_KEY,
  buildBoardShellClasses,
  readBoardSidebarCollapsed,
  writeBoardSidebarCollapsed,
} from "../../../Docs/.vitepress/lib/board-shell.mjs";

function createStorage(initial = {}) {
  const values = new Map(Object.entries(initial));

  return {
    getItem(key) {
      return values.has(key) ? values.get(key) : null;
    },
    setItem(key, value) {
      values.set(key, String(value));
    },
    snapshot() {
      return Object.fromEntries(values);
    },
  };
}

test("readBoardSidebarCollapsed defaults to false without a saved preference",
  () => {
    assert.equal(readBoardSidebarCollapsed(null), false);
    assert.equal(readBoardSidebarCollapsed(createStorage()), false);
    assert.equal(
      readBoardSidebarCollapsed(createStorage({
        [BOARD_SIDEBAR_COLLAPSED_STORAGE_KEY]: "unexpected",
      })),
      false
    );
  });

test("readBoardSidebarCollapsed returns true for a persisted true value", () => {
  const storage = createStorage({
    [BOARD_SIDEBAR_COLLAPSED_STORAGE_KEY]: "true",
  });

  assert.equal(readBoardSidebarCollapsed(storage), true);
});

test("writeBoardSidebarCollapsed persists string booleans", () => {
  const storage = createStorage();

  writeBoardSidebarCollapsed(storage, true);
  writeBoardSidebarCollapsed(storage, false);

  assert.deepEqual(storage.snapshot(), {
    [BOARD_SIDEBAR_COLLAPSED_STORAGE_KEY]: "false",
  });
});

test("buildBoardShellClasses only applies shell classes on board pages", () => {
  assert.deepEqual(
    buildBoardShellClasses({ board: true, collapsed: true }),
    {
      "board-shell-layout": true,
      "board-sidebar-collapsed": true,
    }
  );

  assert.deepEqual(
    buildBoardShellClasses({ board: true, collapsed: false }),
    {
      "board-shell-layout": true,
      "board-sidebar-collapsed": false,
    }
  );

  assert.deepEqual(
    buildBoardShellClasses({ board: false, collapsed: true }),
    {
      "board-shell-layout": false,
      "board-sidebar-collapsed": false,
    }
  );
});
