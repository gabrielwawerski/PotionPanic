import { cloudflareTest } from '@cloudflare/vitest-pool-workers';
import { defineConfig, defineProject } from 'vitest/config';

export default defineConfig({
  test: {
    projects: [
      defineProject({
        test: {
          name: 'protocol',
          include: ['test/protocol.test.ts', 'test/issue-token.test.ts'],
          environment: 'node'
        }
      }),
      defineProject({
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
          name: 'workers',
          include: ['test/**/*.test.ts', 'tests/**/*.test.ts'],
          exclude: ['test/protocol.test.ts', 'test/issue-token.test.ts'],
          pool: '@cloudflare/vitest-pool-workers'
        }
      })
    ]
  }
});
