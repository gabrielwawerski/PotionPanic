import { readFile } from 'node:fs/promises';
import { fileURLToPath, pathToFileURL } from 'node:url';

const coordinationServiceName = 'potion-panic-coordination';
const defaultConfigPath = fileURLToPath(
  new URL('../../../coordination.json', import.meta.url)
);

function normalizeServerBaseUrl(value) {
  let url;

  try {
    url = new URL(value);
  } catch {
    throw new Error('The coordination config contains an invalid serverBaseUrl.');
  }

  if (url.protocol !== 'https:' && url.protocol !== 'http:') {
    throw new Error('The coordination serverBaseUrl must use HTTP or HTTPS.');
  }

  if (url.search || url.hash) {
    throw new Error('The coordination serverBaseUrl must not contain a query or fragment.');
  }

  return url.toString().replace(/\/$/u, '');
}

export async function loadServerBaseUrl(configPath = defaultConfigPath) {
  let config;

  try {
    config = JSON.parse(await readFile(configPath, 'utf8'));
  } catch (error) {
    throw new Error(`Unable to read coordination config: ${error.message}`);
  }

  if (typeof config?.serverBaseUrl !== 'string' || config.serverBaseUrl.trim() === '') {
    throw new Error('The coordination config must define serverBaseUrl.');
  }

  return normalizeServerBaseUrl(config.serverBaseUrl);
}

export async function verifyServerHealth(serverBaseUrl, fetchImpl = fetch) {
  const baseUrl = normalizeServerBaseUrl(serverBaseUrl);
  const response = await fetchImpl(`${baseUrl}/health`, { method: 'GET' });

  if (!response.ok) {
    throw new Error(`Coordination health check failed with HTTP ${response.status}.`);
  }

  const health = await response.json();
  if (health?.service !== coordinationServiceName) {
    throw new Error('Unexpected coordination service response.');
  }

  if (typeof health.serverTime !== 'string' || Number.isNaN(Date.parse(health.serverTime))) {
    throw new Error('Coordination health response has an invalid serverTime.');
  }

  return health;
}

export async function issueDeveloperToken({
  serverBaseUrl,
  displayName,
  adminToken,
  fetchImpl = fetch
}) {
  if (typeof displayName !== 'string' || displayName.trim() === '') {
    throw new Error('A non-empty developer display name is required.');
  }

  if (typeof adminToken !== 'string' || adminToken === '') {
    throw new Error('ADMIN_TOKEN is required.');
  }

  const baseUrl = normalizeServerBaseUrl(serverBaseUrl);
  const projectId = process.env.COORDINATION_PROJECT_ID ?? 'potion-panic';
  const response = await fetchImpl(
    `${baseUrl}/v1/projects/${encodeURIComponent(projectId)}/developers`,
    {
      method: 'POST',
      headers: {
        authorization: `Bearer ${adminToken}`,
        'content-type': 'application/json'
      },
      body: JSON.stringify({ displayName })
    }
  );

  if (!response.ok) {
    throw new Error(`Developer token issuance failed with HTTP ${response.status}.`);
  }

  return response.json();
}

export async function runCli({
  args = process.argv.slice(2),
  env = process.env,
  log = console.log,
  error = console.error
} = {}) {
  const [displayName, ...unexpectedArgs] = args;
  if (displayName === undefined || unexpectedArgs.length > 0) {
    error('Usage: npm run issue-dev-token -- <display-name>');
    return 1;
  }

  if (env.ADMIN_TOKEN === undefined || env.ADMIN_TOKEN === '') {
    error('ADMIN_TOKEN is required.');
    return 1;
  }

  try {
    const serverBaseUrl = await loadServerBaseUrl(
      env.COORDINATION_CONFIG_PATH ?? defaultConfigPath
    );
    await verifyServerHealth(serverBaseUrl);
    const result = await issueDeveloperToken({
      serverBaseUrl,
      displayName,
      adminToken: env.ADMIN_TOKEN
    });

    log(`Developer ID: ${result.developerId}`);
    log(`Display name: ${result.displayName}`);
    log('Developer token (displayed once):');
    log(result.developerToken);
    return 0;
  } catch (caughtError) {
    error(caughtError instanceof Error ? caughtError.message : String(caughtError));
    return 1;
  }
}

const isMainModule = process.argv[1] !== undefined
  && import.meta.url === pathToFileURL(process.argv[1]).href;

if (isMainModule) {
  process.exitCode = await runCli();
}
