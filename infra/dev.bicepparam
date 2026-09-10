using './main.bicep'

param environmentName = 'dev'

// App Service quota for this plan tier is granted per region; the data and monitoring stay in the resource group's region.
param hostingLocation = 'ukwest'

// Secrets come from the shell that runs the deployment and are never written to this repository.
param sqlAdminLogin = readEnvironmentVariable('SQL_ADMIN_LOGIN')
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param jwtSigningKey = readEnvironmentVariable('JWT_SIGNING_KEY')
