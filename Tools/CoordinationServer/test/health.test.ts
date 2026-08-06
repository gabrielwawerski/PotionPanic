import { SELF } from 'cloudflare:test';
import { describe, expect, it } from 'vitest';

describe('GET /health', () => {
  it('returns only the public service identity and server time', async () => {
    const response = await SELF.fetch('https://example.test/health');

    expect(response.status).toBe(200);
    expect(response.headers.get('content-type')).toContain('application/json');
    await expect(response.json()).resolves.toEqual({
      service: 'potion-panic-coordination',
      serverTime: expect.any(String)
    });
  });

  it('does not expose health on other HTTP methods', async () => {
    const response = await SELF.fetch('https://example.test/health', { method: 'POST' });

    expect(response.status).toBe(501);
  });
});
