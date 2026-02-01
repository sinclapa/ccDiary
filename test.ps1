$databaseServer="sql-ccdiary-dev.database.windows.net"
$databaseName="sqldb-ccdiary-dev"
$tenantId="7cb83658-8643-49c2-a04d-ace87eee8784"
$entraClientId="08cfaa38-cb2a-4502-856a-221e71aa7d75"
$entraApplicationIdURI="api://08cfaa38-cb2a-4502-856a-221e71aa7d75"
$containerAppName="ca-ccdiary-dev"
$resourceGroupName="rg-ccdiary-dev"

# Build connection string safely (escape inner quotes for Authentication)
$connStr = "Server=tcp:$($databaseServer),1433;Initial Catalog=$($databaseName);Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=`"Active Directory Default`";"

# Prepare environment variables as an array so PowerShell passes each as a separate argument
$envVars = @(
        "Entra__TenantId=$tenantId",
        "Entra__ClientId=$entraClientId",
        "Entra__ApplicationIdUri=$entraApplicationIdURI",
        "ASPNETCORE_ENVIRONMENT=UAT"
)

az containerapp update `
    --name $containerAppName `
    --resource-group $resourceGroupName `
    --output none `
    --set-env-vars $envVars
