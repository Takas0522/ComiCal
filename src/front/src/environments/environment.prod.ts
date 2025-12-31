export const environment = {
  production: true,
  gapiClientId: process.env['GOOGLE_OAUTH_CLIENT_ID'] || '',
  // Note: blobBaseUrl should be configured via Azure Static Web Apps configuration
  // and injected at build time to avoid exposing resource names in source code
  blobBaseUrl: process.env['BLOB_BASE_URL'] || 'https://comicalstoragedev.blob.core.windows.net/images'
};
