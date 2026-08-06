import type { Env } from './env';

export { CoordinationObject } from './coordination-object';

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    if (request.method === 'GET' && url.pathname === '/health') {
      return Response.json({
        service: 'potion-panic-coordination',
        serverTime: new Date().toISOString()
      });
    }

    const projectId = projectIdFromPath(url.pathname);
    if (projectId !== null) {
      return env.COORDINATION_OBJECT.get(
        env.COORDINATION_OBJECT.idFromName(projectId)
      ).fetch(request);
    }

    return new Response('Not implemented', { status: 501 });
  }
};

function projectIdFromPath(pathname: string): string | null {
  const match = /^\/v1\/projects\/([^/]+)(?:\/|$)/.exec(pathname);
  if (match === null) {
    return null;
  }

  try {
    const projectId = decodeURIComponent(match[1]);
    return projectId.length > 0 ? projectId : null;
  } catch {
    return null;
  }
}
