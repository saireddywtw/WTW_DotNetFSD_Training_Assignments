# Configuration Guide

This guide explains how to configure the Durable Job Processor for different environments.

## Environment Configurations

### Local Development with Mock Data (Default)

Use this configuration for local testing without a Dataverse instance.

**In `Program.cs`, uncomment the MockDataverseService registration:**

```csharp
// For local testing with mock data
services.AddSingleton<IDataverseService, MockDataverseService>();
```

**Comment out the real DataverseService:**

```csharp
// For production with real Dataverse
// services.AddSingleton<IDataverseService>(sp =>
// {
//     var logger = sp.GetRequiredService<ILogger<DataverseService>>();
//     return new DataverseService(logger, connectionString);
// });
```

### Production with Real Dataverse

Use this configuration when connecting to an actual Dataverse instance.

**In `Program.cs`, use the real DataverseService:**

```csharp
// For production with real Dataverse
var connectionString = Environment.GetEnvironmentVariable("DataverseConnectionString") 
    ?? throw new InvalidOperationException("DataverseConnectionString is required");

services.AddSingleton<IDataverseService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<DataverseService>>();
    return new DataverseService(logger, connectionString);
});
```

**Comment out the mock service:**

```csharp
// For local testing with mock data
// services.AddSingleton<IDataverseService, MockDataverseService>();
```

## Connection Strings

### Dataverse Connection String Formats

#### OAuth (Client Credentials)

```
AuthType=OAuth;
Url=https://your-org.crm.dynamics.com;
ClientId=your-client-id;
ClientSecret=your-client-secret;
```

#### OAuth (Certificate)

```
AuthType=Certificate;
Url=https://your-org.crm.dynamics.com;
ClientId=your-client-id;
CertThumbprint=your-cert-thumbprint;
```

#### Office 365 (User Credentials)

```
AuthType=Office365;
Url=https://your-org.crm.dynamics.com;
Username=user@domain.com;
Password=your-password;
```

### Setting Connection Strings

#### Local Development

Edit `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DataverseConnectionString": "AuthType=OAuth;Url=https://your-org.crm.dynamics.com;ClientId=abc;ClientSecret=xyz;",
    "SCHEDULE_EXPRESSION": "0 */5 * * * *"
  }
}
```

#### Azure Portal

1. Navigate to your Function App
2. Go to **Configuration** > **Application Settings**
3. Add/Edit these settings:
   - **DataverseConnectionString**: Your Dataverse connection string
   - **SCHEDULE_EXPRESSION**: Your timer schedule
   - **AzureWebJobsStorage**: Your Azure Storage connection string

#### Azure CLI

```bash
az functionapp config appsettings set \
  --name func-jobprocessor \
  --resource-group rg-jobprocessor \
  --settings \
    "DataverseConnectionString=AuthType=OAuth;Url=https://your-org.crm.dynamics.com;ClientId=abc;ClientSecret=xyz;" \
    "SCHEDULE_EXPRESSION=0 */5 * * * *"
```

#### Azure Key Vault (Recommended for Production)

Store sensitive values in Azure Key Vault:

```bash
# Store the connection string in Key Vault
az keyvault secret set \
  --vault-name kv-jobprocessor \
  --name DataverseConnectionString \
  --value "AuthType=OAuth;Url=https://..."

# Reference in Function App settings
az functionapp config appsettings set \
  --name func-jobprocessor \
  --resource-group rg-jobprocessor \
  --settings \
    "DataverseConnectionString=@Microsoft.KeyVault(SecretUri=https://kv-jobprocessor.vault.azure.net/secrets/DataverseConnectionString/)"
```

## Timer Schedule Configuration

### CRON Expression Format

```
{second} {minute} {hour} {day} {month} {day-of-week}
```

### Common Schedules

| Description | CRON Expression | Setting Value |
|-------------|----------------|---------------|
| Every 5 minutes | `0 */5 * * * *` | `"SCHEDULE_EXPRESSION": "0 */5 * * * *"` |
| Every 10 minutes | `0 */10 * * * *` | `"SCHEDULE_EXPRESSION": "0 */10 * * * *"` |
| Every 30 minutes | `0 */30 * * * *` | `"SCHEDULE_EXPRESSION": "0 */30 * * * *"` |
| Every hour | `0 0 * * * *` | `"SCHEDULE_EXPRESSION": "0 0 * * * *"` |
| Every 6 hours | `0 0 */6 * * *` | `"SCHEDULE_EXPRESSION": "0 0 */6 * * *"` |
| Daily at 9 AM | `0 0 9 * * *` | `"SCHEDULE_EXPRESSION": "0 0 9 * * *"` |
| Weekdays at 9 AM | `0 0 9 * * 1-5` | `"SCHEDULE_EXPRESSION": "0 0 9 * * 1-5"` |
| First of month at midnight | `0 0 0 1 * *` | `"SCHEDULE_EXPRESSION": "0 0 0 1 * *"` |

### Testing Schedules

For faster testing during development:

```json
{
  "Values": {
    "SCHEDULE_EXPRESSION": "0 */1 * * * *"
  }
}
```

This runs every minute for quick testing.

## Storage Configuration

### Local Development

Use Azure Storage Emulator or Azurite:

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true"
  }
}
```

### Azure Storage Account

Use an actual Azure Storage account:

```json
{
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=mystorageacct;AccountKey=key;EndpointSuffix=core.windows.net"
  }
}
```

## Durable Functions Configuration

Edit `host.json` to configure Durable Functions behavior:

### Default Configuration

```json
{
  "extensions": {
    "durableTask": {
      "hubName": "JobProcessorHub",
      "storageProvider": {
        "type": "AzureStorage"
      },
      "maxConcurrentActivityFunctions": 10,
      "maxConcurrentOrchestratorFunctions": 10
    }
  }
}
```

### High Throughput Configuration

For processing many jobs:

```json
{
  "extensions": {
    "durableTask": {
      "hubName": "JobProcessorHub",
      "storageProvider": {
        "type": "AzureStorage",
        "partitionCount": 16
      },
      "maxConcurrentActivityFunctions": 50,
      "maxConcurrentOrchestratorFunctions": 20
    }
  }
}
```

### Low Concurrency Configuration

For rate-limited scenarios:

```json
{
  "extensions": {
    "durableTask": {
      "hubName": "JobProcessorHub",
      "storageProvider": {
        "type": "AzureStorage"
      },
      "maxConcurrentActivityFunctions": 2,
      "maxConcurrentOrchestratorFunctions": 1
    }
  }
}
```

## Logging Configuration

### Development (Verbose Logging)

```json
{
  "logging": {
    "logLevel": {
      "default": "Debug",
      "Host.Results": "Information",
      "Function": "Debug",
      "Host.Aggregator": "Trace"
    }
  }
}
```

### Production (Minimal Logging)

```json
{
  "logging": {
    "logLevel": {
      "default": "Warning",
      "Host.Results": "Error",
      "Function": "Warning",
      "Host.Aggregator": "Information"
    }
  }
}
```

## Application Insights Configuration

### Enable Application Insights

Add the connection string to your settings:

```json
{
  "Values": {
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=your-key;IngestionEndpoint=https://..."
  }
}
```

### Configure Sampling

In `host.json`:

```json
{
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20,
        "excludedTypes": "Request"
      }
    }
  }
}
```

## Testing Different Configurations

### Configuration for Integration Testing

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DataverseConnectionString": "mock",
    "SCHEDULE_EXPRESSION": "0 */1 * * * *",
    "USE_MOCK_SERVICE": "true"
  }
}
```

### Configuration for Load Testing

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=...",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DataverseConnectionString": "AuthType=OAuth;Url=...",
    "SCHEDULE_EXPRESSION": "0 */1 * * * *"
  }
}
```

With `host.json`:

```json
{
  "extensions": {
    "durableTask": {
      "maxConcurrentActivityFunctions": 100,
      "maxConcurrentOrchestratorFunctions": 50
    }
  }
}
```

## Environment-Specific Settings

### Development

```bash
export ENVIRONMENT=Development
export DataverseConnectionString="mock"
export SCHEDULE_EXPRESSION="0 */1 * * * *"
```

### Staging

```bash
export ENVIRONMENT=Staging
export DataverseConnectionString="AuthType=OAuth;Url=https://staging-org.crm.dynamics.com;..."
export SCHEDULE_EXPRESSION="0 */5 * * * *"
```

### Production

```bash
export ENVIRONMENT=Production
export DataverseConnectionString="@Microsoft.KeyVault(SecretUri=...)"
export SCHEDULE_EXPRESSION="0 */5 * * * *"
```

## Switching Between Configurations

### Quick Switch to Mock (Testing)

1. Open `Program.cs`
2. Uncomment: `services.AddSingleton<IDataverseService, MockDataverseService>();`
3. Comment out: `services.AddSingleton<IDataverseService>(sp => new DataverseService(...));`
4. Run: `func start`

### Quick Switch to Production

1. Open `Program.cs`
2. Comment out: `services.AddSingleton<IDataverseService, MockDataverseService>();`
3. Uncomment: `services.AddSingleton<IDataverseService>(sp => new DataverseService(...));`
4. Update `local.settings.json` with production connection string
5. Run: `func start`

## Validation

### Validate Configuration

Run this to check if settings are correct:

```bash
# Check if function can read settings
func settings list

# Check if storage is accessible
func azure storage list

# Check if app settings are correct (in Azure)
az functionapp config appsettings list \
  --name func-jobprocessor \
  --resource-group rg-jobprocessor
```

### Test Connection

Add this test endpoint to validate Dataverse connection:

```csharp
[Function("TestConnection")]
public async Task<HttpResponseData> TestConnection(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
    FunctionContext executionContext)
{
    var logger = executionContext.GetLogger("TestConnection");
    
    try
    {
        var jobs = await _dataverseService.GetJobsByStatusAsync(JobStatus.InQueued);
        logger.LogInformation("Successfully connected to Dataverse. Found {Count} jobs", jobs.Count);
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync($"Connection successful. Found {jobs.Count} queued jobs.");
        return response;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to connect to Dataverse");
        
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        await response.WriteStringAsync($"Connection failed: {ex.Message}");
        return response;
    }
}
```

## Troubleshooting

### Connection Issues

1. **Check connection string format**
   ```bash
   echo $DataverseConnectionString
   ```

2. **Verify permissions**
   - Ensure the service principal has proper Dataverse permissions

3. **Test connectivity**
   - Use the TestConnection endpoint above

### Timer Not Triggering

1. **Check SCHEDULE_EXPRESSION format**
   ```bash
   # Should be 6 fields: second minute hour day month day-of-week
   echo $SCHEDULE_EXPRESSION
   ```

2. **Verify storage connection**
   ```bash
   # Timer state is stored in Azure Storage
   az storage account show --name mystorageacct
   ```

3. **Check function logs**
   ```bash
   func logs
   ```

## Best Practices

1. **Use Key Vault for secrets** in production
2. **Use managed identities** when possible
3. **Set appropriate concurrency limits** based on Dataverse rate limits
4. **Monitor Application Insights** for performance
5. **Test with mock service** before deploying to production
6. **Use different schedules** for different environments (more frequent in dev)
