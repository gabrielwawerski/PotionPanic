const Encoder = new TextEncoder();
const DeveloperTokenDomain = 'potion-panic-coordination/developer-token/v1';
const SessionTokenDomain = 'potion-panic-coordination/session-token/v1';

export function generateOpaqueToken(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  let binary = '';
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }

  return btoa(binary).replaceAll('+', '-').replaceAll('/', '_').replaceAll('=', '');
}

export function createDeveloperTokenDigest(
  hmacKey: string,
  developerToken: string,
  developerId: string,
  displayName: string
): Promise<string> {
  return createDigest(hmacKey, DeveloperTokenDomain, developerToken, developerId, displayName);
}

export function createSessionTokenDigest(
  hmacKey: string,
  sessionToken: string,
  developerId: string,
  expiresAt: string
): Promise<string> {
  return createDigest(hmacKey, SessionTokenDomain, sessionToken, developerId, expiresAt);
}

export async function createTokenLookup(token: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', Encoder.encode(token));
  return Array.from(
    new Uint8Array(digest),
    (byte) => byte.toString(16).padStart(2, '0')
  ).join('');
}

export function constantTimeEquals(left: string, right: string): boolean {
  const leftBytes = Encoder.encode(left);
  const rightBytes = Encoder.encode(right);
  const length = Math.max(leftBytes.length, rightBytes.length);
  let difference = leftBytes.length ^ rightBytes.length;

  for (let index = 0; index < length; index += 1) {
    difference |= (leftBytes[index] ?? 0) ^ (rightBytes[index] ?? 0);
  }

  return difference === 0;
}

async function createDigest(
  hmacKey: string,
  domain: string,
  token: string,
  developerId: string,
  context: string
): Promise<string> {
  const key = await crypto.subtle.importKey(
    'raw',
    Encoder.encode(hmacKey),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign']
  );
  const signature = await crypto.subtle.sign(
    'HMAC',
    key,
    Encoder.encode(`${domain}\u0000${developerId}\u0000${context}\u0000${token}`)
  );

  return Array.from(new Uint8Array(signature), (byte) => byte.toString(16).padStart(2, '0')).join('');
}
