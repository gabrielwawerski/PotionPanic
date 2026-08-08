import { describe, expect, it } from 'vitest';
import {
  issueDeveloperToken,
  loadServerBaseUrl,
  runCli,
  verifyServerHealth
} from '../scripts/issue-token.mjs';

const configUrl = new URL('./fixtures/coordination-config.json', import.meta.url);

describe('developer token tooling', () => {
  it('requires exactly one developer display name at the CLI boundary', async () => {
    const errors: string[] = [];

    await expect(runCli({
      args: [],
      env: { ADMIN_TOKEN: 'admin-token-for-test' },
      error: (message) => errors.push(String(message))
    })).resolves.toBe(1);
    expect(errors).toEqual(['Usage: npm run issue-dev-token -- <display-name>']);
  });

  it('loads the Worker URL from the coordination config', async () => {
    await expect(loadServerBaseUrl(configUrl)).resolves.toBe(
      'https://coordination.example.workers.dev'
    );
  });

  it('accepts a healthy coordination Worker before issuing credentials', async () => {
    const requests: { input: string; init?: RequestInit }[] = [];
    const fetchImpl = async (input: string, init?: RequestInit) => {
      requests.push({ input, init });
      return new Response(JSON.stringify({
        service: 'potion-panic-coordination',
        serverTime: '2026-08-08T12:00:00.000Z'
      }), { status: 200, headers: { 'content-type': 'application/json' } });
    };

    await expect(verifyServerHealth('https://coordination.example.workers.dev', fetchImpl))
      .resolves.toEqual({
        service: 'potion-panic-coordination',
        serverTime: '2026-08-08T12:00:00.000Z'
      });
    expect(requests[0]!.input).toBe('https://coordination.example.workers.dev/health');
    expect(requests[0]!.init).toEqual({ method: 'GET' });
  });

  it('rejects a Worker that does not identify as the coordination service', async () => {
    const fetchImpl = async () => new Response(JSON.stringify({
      service: 'unexpected-service',
      serverTime: '2026-08-08T12:00:00.000Z'
    }), { status: 200 });

    await expect(verifyServerHealth('https://coordination.example.workers.dev', fetchImpl))
      .rejects.toThrow('Unexpected coordination service response.');
  });

  it('issues a developer token against the verified base URL', async () => {
    const requests: { input: string; init?: RequestInit }[] = [];
    const fetchImpl = async (input: string, init?: RequestInit) => {
      requests.push({ input, init });
      return new Response(JSON.stringify({
        developerId: 'developer-1',
        displayName: 'Rin',
        developerToken: 'token-returned-once'
      }), { status: 201 });
    };

    await expect(issueDeveloperToken({
      serverBaseUrl: 'https://coordination.example.workers.dev/',
      displayName: 'Rin',
      adminToken: 'admin-token-for-test',
      fetchImpl
    })).resolves.toEqual({
      developerId: 'developer-1',
      displayName: 'Rin',
      developerToken: 'token-returned-once'
    });
    expect(requests[0]!.input).toBe(
      'https://coordination.example.workers.dev/v1/projects/potion-panic/developers'
    );
    const headers = requests[0]!.init!.headers as Record<string, string>;
    expect(headers.authorization).toBe('Bearer admin-token-for-test');
    expect(requests[0]!.init!.body).toBe(JSON.stringify({ displayName: 'Rin' }));
  });
});
