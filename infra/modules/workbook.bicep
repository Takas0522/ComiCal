// ============================================================================
// workbook.bicep - ComiCal Application Insights Workbook
// ----------------------------------------------------------------------------
// Deploys a single curated workbook (gallery type microsoft.insights/components)
// with tabs for Overview / API / Batch / Rakuten / Dependencies. The serialized
// notebook JSON is sourced from `workbook.json` via loadTextContent() so the
// workbook content is reviewed as JSON in code review (not as an inline string).
// ============================================================================

@description('Environment short code (dev / prod).')
param env string

@description('Azure region.')
param location string

@description('Application Insights resource ID used as the workbook source.')
param appInsightsId string

@description('Common tags applied to every deployed resource.')
param tags object = {}

var workbookContent = loadTextContent('workbook.json')

resource workbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  // GUID-based name is required for the workbooks RP. Stable per (rg, env).
  name: guid(resourceGroup().id, 'comical-observability', env)
  location: location
  tags: tags
  kind: 'shared'
  properties: {
    displayName: 'ComiCal — Observability (${env})'
    serializedData: workbookContent
    sourceId: appInsightsId
    category: 'workbook'
    version: '1.0'
  }
}

@description('Workbook resource ID.')
output workbookId string = workbook.id

@description('Workbook resource name (GUID).')
output workbookName string = workbook.name
