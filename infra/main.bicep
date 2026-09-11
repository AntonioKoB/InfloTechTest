// Everything the two hosts need on Azure, sized for a development environment. Deploy at resource-group scope:
//
//   az deployment group create --resource-group rg-inflo-dev --template-file infra/main.bicep --parameters infra/dev.bicepparam
//
// The environment name drives every resource name; the secret parameters come from environment variables at deploy time.

targetScope = 'resourceGroup'

@description('Short environment name (dev, test, prod). Appears in every resource name.')
@minLength(2)
@maxLength(10)
param environmentName string

@description('Azure region for the database and monitoring resources. Defaults to the resource group\'s region.')
param location string = resourceGroup().location

@description('Azure region for the App Service plan and the web apps. Plan quota is granted per region and per tier, so the hosting region can differ from the rest.')
param hostingLocation string = location

@description('Administrator login for the SQL logical server.')
@secure()
param sqlAdminLogin string

@description('Administrator password for the SQL logical server.')
@secure()
param sqlAdminPassword string

@description('HS256 signing key for the API\'s JWTs. At least 32 bytes; the API refuses to start with less.')
@secure()
@minLength(32)
param jwtSigningKey string

var workload = 'inflo'
// Web app and SQL server names must be unique across all of Azure; this suffix is stable per resource group.
var uniqueSuffix = uniqueString(resourceGroup().id)

var planName = 'plan-${workload}-${environmentName}'
var apiSiteName = 'app-${workload}-api-${environmentName}-${uniqueSuffix}'
var blazorSiteName = 'app-${workload}-blazor-${environmentName}-${uniqueSuffix}'
var sqlServerName = 'sql-${workload}-${environmentName}-${uniqueSuffix}'
var sqlDatabaseName = 'sqldb-${workload}-${environmentName}'
var logAnalyticsName = 'log-${workload}-${environmentName}'
var appInsightsName = 'appi-${workload}-${environmentName}'

var tags = {
  workload: workload
  environment: environmentName
}

// App Service terminates TLS at its front end, so the apps need forwarded headers or UseHttpsRedirection
// loops. Health check is not active on the Free tier; the path is set for when the plan is scaled up.
var commonSiteConfig = {
  linuxFxVersion: 'DOTNETCORE|10.0'
  alwaysOn: false // Not available on F1.
  healthCheckPath: '/health'
  ftpsState: 'Disabled'
  minTlsVersion: '1.2'
  http20Enabled: true
}

var commonAppSettings = {
  APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
  ASPNETCORE_FORWARDEDHEADERS_ENABLED: 'true'
}

// ---------------------------------------------------------------------------------------------------------
// Monitoring: a Log Analytics workspace and a workspace-based Application Insights component.
// ---------------------------------------------------------------------------------------------------------

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      // Hard ceiling on ingestion (about 3 GB a month).
      dailyQuotaGb: json('0.1')
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

// ---------------------------------------------------------------------------------------------------------
// Database: one logical server, one serverless database.
// ---------------------------------------------------------------------------------------------------------

resource sqlServer 'Microsoft.Sql/servers@2025-01-01' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2025-01-01' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    // Monthly vCore-second allowance; the database pauses instead of billing when it runs out.
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
}

// Allows connections from Azure services only; the API runs the migrations at startup.
resource sqlAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2025-01-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---------------------------------------------------------------------------------------------------------
// Hosting: one Linux plan, two web apps.
// ---------------------------------------------------------------------------------------------------------

resource appServicePlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: planName
  location: hostingLocation
  tags: tags
  kind: 'linux'
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  properties: {
    reserved: true // Linux.
  }
}

resource apiSite 'Microsoft.Web/sites@2025-03-01' = {
  name: apiSiteName
  location: hostingLocation
  tags: tags
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: commonSiteConfig
  }
}

resource apiAppSettings 'Microsoft.Web/sites/config@2025-03-01' = {
  parent: apiSite
  name: 'appsettings'
  properties: union(commonAppSettings, {
    Jwt__SigningKey: jwtSigningKey
  })
}

resource apiConnectionStrings 'Microsoft.Web/sites/config@2025-03-01' = {
  parent: apiSite
  name: 'connectionstrings'
  properties: {
    DefaultConnection: {
      type: 'SQLAzure'
      value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
    }
  }
}

resource blazorSite 'Microsoft.Web/sites@2025-03-01' = {
  name: blazorSiteName
  location: hostingLocation
  tags: tags
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: union(commonSiteConfig, {
      webSocketsEnabled: true // Blazor Server's SignalR circuit.
    })
  }
}

resource blazorAppSettings 'Microsoft.Web/sites/config@2025-03-01' = {
  parent: blazorSite
  name: 'appsettings'
  properties: union(commonAppSettings, {
    Api__BaseUrl: 'https://${apiSite.properties.defaultHostName}'
  })
}

// ---------------------------------------------------------------------------------------------------------
// Outputs: what a deployment pipeline needs to publish the apps and what a person needs to reach them.
// ---------------------------------------------------------------------------------------------------------

output apiSiteName string = apiSite.name
output apiHostName string = apiSite.properties.defaultHostName
output blazorSiteName string = blazorSite.name
output blazorHostName string = blazorSite.properties.defaultHostName
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output appInsightsConnectionString string = appInsights.properties.ConnectionString
