import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const repoRoot = path.resolve(import.meta.dirname, "../../..");
const ticketDetailPath = path.join(
  repoRoot,
  "Docs/.vitepress/theme/components/TicketDetail.vue"
);
const tokenSuggestionInputPath = path.join(
  repoRoot,
  "Docs/.vitepress/theme/components/TokenSuggestionInput.vue"
);

test("ticket detail only shows documentation and affected file lists in read-only mode",
  () => {
    const source = fs.readFileSync(ticketDetailPath, "utf8");

    assert.match(
      source,
      /v-if="readOnly && ticket\.documentation\.length > 0"/,
      "documentation list should be limited to read-only mode"
    );
    assert.match(
      source,
      /v-if="readOnly && ticket\.affectedFiles\.length > 0"/,
      "affected files list should be limited to read-only mode"
    );
  });

test("token suggestion input supports clickable selected tokens", () => {
  const source = fs.readFileSync(tokenSuggestionInputPath, "utf8");

  assert.match(
    source,
    /resolveHref\?: \(value: string\) => string \| null/,
    "selected tokens should support optional href resolution"
  );
  assert.match(
    source,
    /<component[\s\S]*?:is="item\.href \? 'a' : 'span'"/,
    "selected token labels should render as links when an href exists"
  );
});

test("ticket detail resolves published links against the VitePress base url", () => {
  const source = fs.readFileSync(ticketDetailPath, "utf8");

  assert.match(
    source,
    /resolveTicketHref\(value,\s*\{\s*base: import\.meta\.env\.BASE_URL,/,
    "dependency links should include the published site base"
  );
  assert.match(
    source,
    /buildDocumentationHref\(value,\s*import\.meta\.env\.BASE_URL\)/,
    "documentation links should include the published site base"
  );
});
