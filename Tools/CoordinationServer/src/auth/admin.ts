import { constantTimeEquals } from './crypto';

export function hasAdministratorToken(request: Request, administratorToken: string): boolean {
  const token = readBearerValue(request);
  return token !== null && constantTimeEquals(token, administratorToken);
}

export function readBearerToken(request: Request): string | null {
  const token = readBearerValue(request);
  return token !== null && /^[A-Za-z0-9_-]{43}$/.test(token) ? token : null;
}

function readBearerValue(request: Request): string | null {
  const authorization = request.headers.get('authorization');
  if (authorization === null || !authorization.startsWith('Bearer ')) {
    return null;
  }

  const token = authorization.slice('Bearer '.length);
  return token.length > 0 && !/\s/.test(token) ? token : null;
}

export function isDisplayName(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0 && value.length <= 256;
}
