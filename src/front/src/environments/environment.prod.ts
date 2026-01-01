// Environment variables will be replaced during Azure Static Web Apps build process
// See staticwebapp.config.json for environment variable configuration
export const environment = {
  production: true,
  gapiClientId: '#{GOOGLE_OAUTH_CLIENT_ID}#',
  // Note: blobBaseUrl MUST be configured via Azure Static Web Apps configuration
  // The placeholder will be replaced by Azure Static Web Apps build process
  blobBaseUrl: '#{BLOB_BASE_URL}#'
};
