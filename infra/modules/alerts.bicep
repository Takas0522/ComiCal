// ============================================================================
// alerts.bicep - Application Insights & Storage alert rules (Phase 3)
// ----------------------------------------------------------------------------
// Implements the alert catalogue defined in
// docs/specs/oo-init/14-observability-sre.md §14.2 and the Phase 3 plan.
//
// Metric alerts (Microsoft.Insights/metricAlerts):
//   - api-5xx-error-rate            requests/failed > N over 5 min      (sev2)
//   - sql-dependency-failures       dependencies/failed > N over 5 min  (sev2)
//   - storage-availability          Availability < N% over 1 hour       (sev2)
//
// Log alerts (Microsoft.Insights/scheduledQueryRules):
//   - api-p95-latency               percentile(duration,95) > N ms       (sev3)
//   - batch-failures                comical-batch requests success=false (sev1)
//   - batch-volumes-zero            customMetrics batch.volumes_ingested (sev2)
//   - rakuten-rate-limited          customMetrics rakuten.api.rate_limited (sev2)
//
// Thresholds are parameterised so dev/prod can diverge via bicepparam.
// ============================================================================

@description('Common short prefix for all resource names.')
param prefix string

@description('Environment short code (dev / prod).')
param env string

@description('Short region code embedded in resource names.')
param regionShort string

@description('Azure region for log-search alert rules. Metric alerts are global.')
param location string

@description('Application Insights resource ID (scope for AI-based alerts).')
param appInsightsId string

@description('Storage account resource ID (scope for the Availability alert).')
param storageAccountId string

@description('Action Group resource ID receiving every alert.')
param actionGroupId string

@description('API failed requests count threshold over 5 min (sev 2).')
@minValue(0)
param apiFailedRequestsThreshold int = 10

@description('API p95 request duration threshold in milliseconds over 15 min (sev 3).')
@minValue(100)
param apiP95LatencyMs int = 2000

@description('SQL dependency failures count threshold over 5 min (sev 2).')
@minValue(0)
param sqlDependencyFailuresThreshold int = 5

@description('Storage account availability percentage threshold (alert when below) over 1 hour (sev 2).')
@minValue(50)
@maxValue(100)
param storageAvailabilityPercent int = 99

@description('Rakuten API 429 rate-limited count threshold over 5 min (sev 2).')
@minValue(0)
param rakutenRateLimitedThreshold int = 50

@description('Common tags applied to every deployed resource.')
param tags object = {}

var namePrefix = '${prefix}-${env}-${regionShort}-alert'

// ============================================================================
// Metric alerts
// ============================================================================

// 1) API 5xx / failed requests count
resource alertApi5xx 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${namePrefix}-api-5xx'
  location: 'global'
  tags: tags
  properties: {
    description: 'Failed HTTP requests exceed threshold over 5 minutes (potential 5xx surge).'
    severity: 2
    enabled: true
    scopes: [ appInsightsId ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    targetResourceType: 'Microsoft.Insights/components'
    autoMitigate: true
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'failedRequests'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'requests/failed'
          operator: 'GreaterThan'
          threshold: apiFailedRequestsThreshold
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroupId
      }
    ]
  }
}

// 2) SQL dependency failures (filter by dependency/type=SQL dimension)
resource alertSqlDependencyFailures 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${namePrefix}-sql-deps-failed'
  location: 'global'
  tags: tags
  properties: {
    description: 'SQL dependency failures exceed threshold over 5 minutes.'
    severity: 2
    enabled: true
    scopes: [ appInsightsId ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    targetResourceType: 'Microsoft.Insights/components'
    autoMitigate: true
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'sqlDependencyFailures'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'dependencies/failed'
          operator: 'GreaterThan'
          threshold: sqlDependencyFailuresThreshold
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
          dimensions: [
            {
              name: 'dependency/type'
              operator: 'Include'
              values: [ 'SQL' ]
            }
          ]
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroupId
      }
    ]
  }
}

// 3) Storage account availability < N% over 1h
resource alertStorageAvailability 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${namePrefix}-storage-availability'
  location: 'global'
  tags: tags
  properties: {
    description: 'Storage account availability dropped below ${storageAvailabilityPercent}% over the last hour.'
    severity: 2
    enabled: true
    scopes: [ storageAccountId ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT1H'
    targetResourceType: 'Microsoft.Storage/storageAccounts'
    autoMitigate: true
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'availability'
          metricNamespace: 'Microsoft.Storage/storageAccounts'
          metricName: 'Availability'
          operator: 'LessThan'
          threshold: storageAvailabilityPercent
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroupId
      }
    ]
  }
}

// ============================================================================
// Log search alerts (scheduledQueryRules v2)
// ============================================================================

// 4) API p95 latency (App Insights metric `requests/duration` only exposes Avg
//    in metric alerts, so we use a log-search rule for Percentile aggregation.)
resource alertApiP95Latency 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: '${namePrefix}-api-p95-latency'
  location: location
  tags: tags
  properties: {
    displayName: 'API p95 latency exceeded'
    description: 'API request duration p95 above ${apiP95LatencyMs} ms over 15 minutes.'
    severity: 3
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    scopes: [ appInsightsId ]
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: 'requests\n| where cloud_RoleName startswith "comical-api"\n| summarize p95 = percentile(duration, 95)\n| where p95 > ${apiP95LatencyMs}'
          operator: 'GreaterThan'
          threshold: 0
          timeAggregation: 'Count'
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [ actionGroupId ]
    }
  }
}

// 5) Batch failures
resource alertBatchFailures 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: '${namePrefix}-batch-failures'
  location: location
  tags: tags
  properties: {
    displayName: 'Batch (comical-batch) request failures'
    description: 'One or more failed requests recorded for the Durable Functions batch role.'
    severity: 1
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT1H'
    scopes: [ appInsightsId ]
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: 'requests\n| where cloud_RoleName == "comical-batch" and success == false\n| summarize failed = count() by bin(timestamp, 1h)\n| where failed > 0'
          operator: 'GreaterThan'
          threshold: 0
          timeAggregation: 'Count'
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [ actionGroupId ]
    }
  }
}

// 6) Batch volumes ingested = 0 (daily SLO at 04:00 JST = 19:00 UTC).
//    scheduledQueryRules don't accept cron schedules. We evaluate hourly with a
//    24h window so the alert fires at most ~1h after the SLA breach.
resource alertBatchVolumesZero 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: '${namePrefix}-batch-volumes-zero'
  location: location
  tags: tags
  properties: {
    displayName: 'Batch ingested zero volumes in last 24h'
    description: 'No batch.volumes_ingested customMetric recorded in the last 24h. Daily 03:00 JST batch likely failed to publish results.'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT1H'
    windowSize: 'P1D'
    scopes: [ appInsightsId ]
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: 'customMetrics\n| where name == "batch.volumes_ingested"\n| summarize total = sum(value)\n| where isnull(total) or total == 0'
          operator: 'GreaterThan'
          threshold: 0
          timeAggregation: 'Count'
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [ actionGroupId ]
    }
  }
}

// 7) Rakuten 429 rate
resource alertRakutenRateLimited 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: '${namePrefix}-rakuten-429'
  location: location
  tags: tags
  properties: {
    displayName: 'Rakuten Books API rate-limited (429)'
    description: 'Rakuten 429 responses exceeded ${rakutenRateLimitedThreshold} in 5 minutes.'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    scopes: [ appInsightsId ]
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: 'customMetrics\n| where name == "rakuten.api.rate_limited"\n| summarize hits = sum(value) by bin(timestamp, 5m)\n| where hits > ${rakutenRateLimitedThreshold}'
          operator: 'GreaterThan'
          threshold: 0
          timeAggregation: 'Count'
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [ actionGroupId ]
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('IDs of every alert rule deployed by this module (useful for tagging/audit).')
output alertRuleIds array = [
  alertApi5xx.id
  alertSqlDependencyFailures.id
  alertStorageAvailability.id
  alertApiP95Latency.id
  alertBatchFailures.id
  alertBatchVolumesZero.id
  alertRakutenRateLimited.id
]
