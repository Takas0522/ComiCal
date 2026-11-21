@description('Resource name prefix following CAF convention')
param prefix string

@description('Environment short code')
param env string

@description('Monthly budget amount in the billing currency (JPY)')
param amount int

@description('Budget start date (first day of a month, yyyy-MM-01). Cannot be changed after creation.')
param startDate string

@description('Action group notified when a threshold is crossed')
param actionGroupId string

@description('Additional e-mail recipients')
param contactEmails array = []

var budgetName = '${prefix}-${env}-jpe-budget'

// Resource-group scoped monthly cost budget. Budgets themselves are free; they only notify
// (they never stop resources). Thresholds: 80% actual, 100% actual, 100% forecast.
resource budget 'Microsoft.Consumption/budgets@2023-05-01' = {
  name: budgetName
  properties: {
    category: 'Cost'
    amount: amount
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: startDate
    }
    notifications: {
      actual80: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 80
        thresholdType: 'Actual'
        contactEmails: contactEmails
        contactGroups: [
          actionGroupId
        ]
      }
      actual100: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 100
        thresholdType: 'Actual'
        contactEmails: contactEmails
        contactGroups: [
          actionGroupId
        ]
      }
      forecast100: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 100
        thresholdType: 'Forecasted'
        contactEmails: contactEmails
        contactGroups: [
          actionGroupId
        ]
      }
    }
  }
}

@description('Budget resource ID')
output budgetId string = budget.id
