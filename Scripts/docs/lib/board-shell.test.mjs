import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

import {
  BOARD_SIDEBAR_COLLAPSED_STORAGE_KEY,
  buildBoardShellClasses,
  readBoardSidebarCollapsed,
  writeBoardSidebarCollapsed,
} from "../../../Docs/.vitepress/lib/board-shell.mjs";

const boardStyles = fs.readFileSync(
  path.resolve("Docs/.vitepress/theme/styles/board.css"),
  "utf8"
);

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

test("board shell large-screen navbar title does not force sidebar width", () => {
  const largeScreenTitleBlock = boardStyles.match(
    /@media \(min-width: 1440px\)\s*\{[\s\S]*?\.board-shell-layout \.VPNavBar\.has-sidebar \.title \{([\s\S]*?)\n  \}/
  );

  assert.ok(largeScreenTitleBlock, "expected large-screen board title block");
  assert.doesNotMatch(
    largeScreenTitleBlock[1],
    /width:\s*var\(--vp-sidebar-width\)/,
    "board page should use the normal VitePress title width at large sizes"
  );
  assert.match(
    boardStyles,
    /\.board-shell-layout\.board-sidebar-collapsed \.VPSidebar \{\s*width:\s*var\(--vp-sidebar-width\);/,
    "collapsed board sidebar width rule should still exist"
  );
});

test("board shell does not override navbar divider sidebar padding", () => {
  const dividerBlock = boardStyles.match(
    /\.board-shell-layout \.VPNavBar\.has-sidebar \.divider \{([\s\S]*?)\n  \}/
  );

  assert.ok(dividerBlock, "expected board divider block");
  assert.doesNotMatch(
    dividerBlock[1],
    /padding-left:\s*var\(--vp-sidebar-width\)/,
    "board page should use the normal VitePress divider padding"
  );
});
