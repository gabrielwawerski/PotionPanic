import type { Env } from './env';

export { CoordinationObject } from './coordination-object';

export default {
  async fetch(request: Request, _env: Env): Promise<Response> {
    if (request.method === 'GET' && new URL(request.url).pathname === '/health') {
      return Response.json({
        service: 'potion-panic-coordination',
        serverTime: new Date().toISOString()
      });
    }

    return new Response('Not implemented', { status: 501 });
  }
};
