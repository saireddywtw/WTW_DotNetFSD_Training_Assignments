# Durable Job Processor - Azure Functions

This project implements a timer-triggered durable orchestrator function that processes jobs from Microsoft Dataverse on a scheduled basis.

## Features

- **Timer-Triggered Orchestration**: Automatically runs every 5 minutes (configurable)
- **Dataverse Integration**: Queries and updates job status in Microsoft Dataverse
- **Durable Orchestration**: Uses Azure Durable Functions for reliable, scalable job processing
- **Error Handling**: Comprehensive error handling with retry logic
- **Status Management**: Tracks jobs through multiple states (InQueued, InProgress, Completed, Failed)
- **Parallel Processing**: Uses fan-out/fan-in pattern for efficient batch processing
- **Logging**: Detailed logging throughout the workflow

## Architecture

### Components

1. **JobProcessingTimerTrigger**: Timer-triggered function that starts the orchestration every 5 minutes
2. **JobProcessingOrchestrator**: Orchestrator that coordinates the job processing workflow
3. **GetQueuedJobsActivity**: Activity that queries Dataverse for jobs with "InQueued" status
4. **ProcessJobActivity**: Activity that performs the main business work for each job
5. **UpdateJobStatusActivity**: Activity that updates job status in Dataverse
6. **DataverseService**: Service layer for Dataverse operations

### Workflow

```
Timer Trigger (Every 5 min)
    ↓
Start Orchestration
    ↓
Query Dataverse for InQueued Jobs
    ↓
For Each Job (Parallel Processing):
    ├─ Update Status to InProgress
    ├─ Process Job (with retry logic)
    ├─ Update Status to Completed/Failed
    └─ Log Results
    ↓
Complete Orchestration
```

## Prerequisites

- .NET 8.0 SDK or later
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azure Storage Account (for Durable Functions state)
- Microsoft Dataverse instance (optional - mock implementation provided)

## Configuration

### local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DataverseConnectionString": "AuthType=OAuth;Url=https://your-org.crm.dynamics.com;ClientId=your-client-id;ClientSecret=your-client-secret;",
    "SCHEDULE_EXPRESSION": "0 */5 * * * *"
  }
}
```

### Configuration Parameters

- **AzureWebJobsStorage**: Connection string for Azure Storage (used by Durable Functions)
- **FUNCTIONS_WORKER_RUNTIME**: Set to "dotnet-isolated" for .NET isolated worker process
- **DataverseConnectionString**: Connection string for Microsoft Dataverse
- **SCHEDULE_EXPRESSION**: CRON expression for timer schedule (default: every 5 minutes)

### Schedule Expression Format

The timer uses CRON expressions with 6 fields:
```
{second} {minute} {hour} {day} {month} {day-of-week}
```

Examples:
- `0 */5 * * * *` - Every 5 minutes
- `0 */10 * * * *` - Every 10 minutes
- `0 0 * * * *` - Every hour
- `0 0 9 * * *` - Every day at 9:00 AM

## Installation

1. Clone the repository
2. Navigate to the DurableJobProcessor directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```

## Running Locally

1. Start Azure Storage Emulator (or ensure connection to Azure Storage):
   ```bash
   # Windows
   AzureStorageEmulator.exe start
   
   # Or use Azurite
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

2. Run the function:
   ```bash
   dotnet build
   func start
   ```

3. The timer will trigger automatically based on the schedule
4. You can also manually trigger the orchestration using the Durable Functions HTTP API

## Dataverse Integration

### Current Implementation

The current implementation includes a **mock/stub** version of the Dataverse service. This allows the solution to run and be tested without requiring an actual Dataverse instance.

### Production Implementation

To connect to a real Dataverse instance:

1. **Update the connection string** in `local.settings.json`:
   ```json
   "DataverseConnectionString": "AuthType=OAuth;Url=https://your-org.crm.dynamics.com;ClientId=your-client-id;ClientSecret=your-client-secret;"
   ```

2. **Implement the Dataverse SDK calls** in `DataverseService.cs`:
   - Uncomment and complete the commented code sections
   - Use `Microsoft.PowerPlatform.Dataverse.Client` SDK
   - Map Dataverse entities to the Job model

3. **Configure Azure AD App Registration**:
   - Create an App Registration in Azure AD
   - Grant permissions to Dataverse
   - Use the Client ID and Client Secret in the connection string

### Dataverse Entity Schema

Expected Dataverse entity structure:
- **Entity Name**: `job_entity` (customize as needed)
- **Fields**:
  - `id` (Guid): Primary key
  - `name` (String): Job name
  - `statuscode` (OptionSet): Job status (0=InQueued, 1=InProgress, 2=Completed, 3=Failed)
  - `description` (String): Job description
  - `starttime` (DateTime): Processing start time
  - `endtime` (DateTime): Processing end time
  - `errormessage` (String): Error details if failed

## Error Handling

The solution implements multiple levels of error handling:

1. **Activity Level**: Each activity has try-catch blocks
2. **Orchestrator Level**: Retry policies for transient failures
3. **Status Updates**: Jobs are marked as Failed if processing errors occur
4. **Logging**: Comprehensive logging at all levels

### Retry Configuration

Activities are configured with retry policies:
- **Max Retries**: 3 attempts
- **Initial Retry Interval**: 5 seconds
- **Backoff Coefficient**: 2.0 (exponential backoff)

## Monitoring and Logging

### Application Insights

The function is configured to use Application Insights for monitoring:
- Function execution traces
- Custom metrics
- Exception tracking
- Dependency tracking

### Logging Levels

- **Information**: Normal operation flow
- **Warning**: Non-critical issues (e.g., job failures)
- **Error**: Critical errors requiring attention

### Monitoring Orchestrations

You can monitor orchestration instances using:
1. Azure Portal - Durable Functions monitor
2. Application Insights - Search and Analytics
3. Durable Functions HTTP API endpoints

## Deployment

### Deploy to Azure

1. Create Azure resources:
   ```bash
   # Create resource group
   az group create --name rg-jobprocessor --location eastus
   
   # Create storage account
   az storage account create --name stjobprocessor --resource-group rg-jobprocessor --location eastus
   
   # Create function app
   az functionapp create --resource-group rg-jobprocessor --consumption-plan-location eastus \
     --runtime dotnet-isolated --functions-version 4 --name func-jobprocessor --storage-account stjobprocessor
   ```

2. Configure application settings:
   ```bash
   az functionapp config appsettings set --name func-jobprocessor --resource-group rg-jobprocessor \
     --settings "DataverseConnectionString=<your-connection-string>" \
                "SCHEDULE_EXPRESSION=0 */5 * * * *"
   ```

3. Deploy the function:
   ```bash
   func azure functionapp publish func-jobprocessor
   ```

## Customization

### Adjusting the Schedule

Modify the `SCHEDULE_EXPRESSION` in `local.settings.json` or Azure Function App Settings.

### Customizing Business Logic

Edit the `ProcessJobActivity.cs` file, specifically the `PerformBusinessLogic` method:

```csharp
private string PerformBusinessLogic(Job job)
{
    // Add your custom business logic here
    // Examples:
    // - Call external APIs
    // - Transform data
    // - Perform calculations
    // - Generate reports
    return $"Processed: {job.Name}";
}
```

### Adjusting Concurrency

Modify `host.json` to adjust how many jobs are processed concurrently:

```json
"extensions": {
  "durableTask": {
    "maxConcurrentActivityFunctions": 10,
    "maxConcurrentOrchestratorFunctions": 10
  }
}
```

## Testing

### Manual Testing

1. Start the function locally
2. Add jobs with "InQueued" status to your Dataverse instance (or modify the mock to return test data)
3. Wait for the timer to trigger or manually start an orchestration
4. Monitor logs to see the processing flow

### Testing with Mock Data

Modify `DataverseService.cs` to return test jobs:

```csharp
public async Task<List<Job>> GetJobsByStatusAsync(JobStatus status)
{
    // Return test data
    return new List<Job>
    {
        new Job { Id = Guid.NewGuid(), Name = "Test Job 1", Status = JobStatus.InQueued },
        new Job { Id = Guid.NewGuid(), Name = "Test Job 2", Status = JobStatus.InQueued }
    };
}
```

## Troubleshooting

### Common Issues

1. **Storage Emulator Not Running**
   - Error: "No connection could be made"
   - Solution: Start Azure Storage Emulator or configure Azure Storage connection string

2. **Timer Not Triggering**
   - Check SCHEDULE_EXPRESSION format
   - Verify AzureWebJobsStorage connection
   - Review function logs for errors

3. **Dataverse Connection Issues**
   - Verify connection string format
   - Check Azure AD permissions
   - Ensure network connectivity to Dataverse instance

4. **Orchestration Failures**
   - Check Application Insights for detailed errors
   - Review activity function logs
   - Verify retry policy settings

## Project Structure

```
DurableJobProcessor/
├── Activities/
│   ├── GetQueuedJobsActivity.cs
│   ├── ProcessJobActivity.cs
│   └── UpdateJobStatusActivity.cs
├── Models/
│   └── Job.cs
├── Orchestrators/
│   └── JobProcessingOrchestrator.cs
├── Services/
│   ├── IDataverseService.cs
│   └── DataverseService.cs
├── Triggers/
│   └── JobProcessingTimerTrigger.cs
├── DurableJobProcessor.csproj
├── host.json
├── local.settings.json
├── Program.cs
└── README.md
```

## License

This project is provided as a training example and can be modified as needed.

## Support

For issues or questions, please refer to:
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Durable Functions Documentation](https://docs.microsoft.com/azure/azure-functions/durable/)
- [Dataverse Documentation](https://docs.microsoft.com/power-apps/developer/data-platform/)
