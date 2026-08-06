import { cloudflareTest } from '@cloudflare/vitest-pool-workers';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [cloudflareTest({
    wrangler: { configPath: './wrangler.jsonc' },
    miniflare: {
      bindings: {
        ADMIN_TOKEN: 'test-admin-token',
        TOKEN_HMAC_KEY: 'test-hmac-key'
      }
    }
  })],
  test: {
    pool: '@cloudflare/vitest-pool-workers'
  }
});
