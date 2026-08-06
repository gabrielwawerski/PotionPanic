const [serverBaseUrl, displayName] = process.argv.slice(2);
const adminToken = process.env.ADMIN_TOKEN;

if (serverBaseUrl === undefined || displayName === undefined || adminToken === undefined) {
  console.error('Usage: ADMIN_TOKEN=<secret> node scripts/issue-token.mjs <server-base-url> <display-name>');
  process.exitCode = 1;
} else {
  const baseUrl = new URL(serverBaseUrl);
  const projectId = process.env.COORDINATION_PROJECT_ID ?? 'potion-panic';
  const response = await fetch(`${baseUrl.toString().replace(/\/$/, '')}/v1/projects/${encodeURIComponent(projectId)}/developers`, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${adminToken}`,
      'content-type': 'application/json'
    },
    body: JSON.stringify({ displayName })
  });

  if (!response.ok) {
    throw new Error(`Developer token issuance failed with HTTP ${response.status}.`);
  }

  const result = await response.json();
  console.log(`Developer ID: ${result.developerId}`);
  console.log(`Display name: ${result.displayName}`);
  console.log('Developer token (displayed once):');
  console.log(result.developerToken);
}
