export interface DeveloperTokenResult {
  developerId: string;
  displayName: string;
  developerToken: string;
}

export type FetchImplementation = (
  input: string,
  init?: RequestInit
) => Promise<Response>;

export function loadServerBaseUrl(configPath?: string | URL): Promise<string>;

export function verifyServerHealth(
  serverBaseUrl: string,
  fetchImpl?: FetchImplementation
): Promise<{ service: string; serverTime: string }>;

export function issueDeveloperToken(options: {
  serverBaseUrl: string;
  displayName: string;
  adminToken: string;
  fetchImpl?: FetchImplementation;
}): Promise<DeveloperTokenResult>;

export function runCli(options?: {
  args?: string[];
  env?: Record<string, string | undefined>;
  log?: (...values: unknown[]) => void;
  error?: (...values: unknown[]) => void;
}): Promise<number>;
