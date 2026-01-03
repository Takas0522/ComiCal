export const environment = {
  production: true,
  gapiClientId: '',
  // Note: blobBaseUrl MUST be configured via Azure Static Web Apps configuration
  // or build-time environment variables. Never hardcode resource names in source code.
  blobBaseUrl: 'https://manrem.devtakas.jp/images',  // 本番環境のURL
  apiUrl: '/api'  // Static Web AppsからContainer Apps APIへのプロキシ経由でアクセス
};
