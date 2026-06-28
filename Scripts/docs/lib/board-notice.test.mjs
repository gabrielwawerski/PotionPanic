import assert from "node:assert/strict";
import test from "node:test";

import {READ_ONLY_BOARD_NOTICE} from "../../../Docs/.vitepress/lib/board-notice.mjs";

test("READ_ONLY_BOARD_NOTICE points users to supported docs commands", () => {
  assert.match(READ_ONLY_BOARD_NOTICE, /npm run docs:dev/);
  assert.match(READ_ONLY_BOARD_NOTICE, /npm run docs:ui/);
  assert.doesNotMatch(READ_ONLY_BOARD_NOTICE, /docs-ui\.ps1/);
});
