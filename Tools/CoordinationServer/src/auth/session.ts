export const LeaseTtlSeconds = 120;
export const ReservationTtlSeconds = 30 * 60;
export const SessionTtlMilliseconds = 24 * 60 * 60 * 1000;

export function sessionExpiry(serverTime: Date): string {
  return new Date(serverTime.getTime() + SessionTtlMilliseconds).toISOString();
}

export function isExpired(expiresAt: string, serverTime: Date): boolean {
  return Date.parse(expiresAt) <= serverTime.getTime();
}
