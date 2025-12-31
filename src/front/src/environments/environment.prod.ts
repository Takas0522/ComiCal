export const environment = {
  production: true,
  gapiClientId: process.env['GOOGLE_OAUTH_CLIENT_ID'] || '',
  // Note: blobBaseUrl MUST be configured via Azure Static Web Apps configuration
  // or build-time environment variables. Never hardcode resource names in source code.
  blobBaseUrl: process.env['BLOB_BASE_URL'] || ''
};
