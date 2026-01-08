# Demo: Timer-Triggered Durable Job Processor

This document demonstrates how the timer-triggered durable orchestrator function works.

## Overview

The system processes jobs from Dataverse on a scheduled basis (every 5 minutes by default). Here's how it operates:

## Step-by-Step Workflow

### 1. Timer Trigger Activation

**Every 5 minutes**, the `JobProcessingTimerTrigger` function is activated by the Azure Functions timer.

```
[2026-01-08 10:00:00] Timer triggered - Starting job processing
[2026-01-08 10:00:00] Started orchestration with ID: abc123
```

### 2. Orchestration Start

The timer trigger starts a new instance of `JobProcessingOrchestrator`.

```
[2026-01-08 10:00:01] Job processing orchestration started
```

### 3. Query Dataverse

The orchestrator calls `GetQueuedJobsActivity` to retrieve all jobs with "InQueued" status from Dataverse.

**Sample Dataverse Query:**
```sql
SELECT id, name, statuscode, description, starttime, endtime, errormessage, retrycount
FROM job_entity
WHERE statuscode = 0  -- InQueued
```

**Result:**
```
[2026-01-08 10:00:02] Querying Dataverse for jobs with InQueued status
[2026-01-08 10:00:03] Retrieved 3 jobs to process
```

### 4. Parallel Job Processing

The orchestrator processes each job in parallel using the fan-out/fan-in pattern.

**For each job:**

#### Job 1: "Data Import Job"

```
[2026-01-08 10:00:04] Processing job abc-111: Data Import Job
[2026-01-08 10:00:04] Job abc-111 status updated to InProgress
[2026-01-08 10:00:04] Processing business logic for job abc-111
[2026-01-08 10:00:06] Business logic completed for job abc-111
[2026-01-08 10:00:06] Job abc-111 status updated to Completed
[2026-01-08 10:00:06] Job abc-111 completed successfully
```

#### Job 2: "Report Generation"

```
[2026-01-08 10:00:04] Processing job abc-222: Report Generation
[2026-01-08 10:00:04] Job abc-222 status updated to InProgress
[2026-01-08 10:00:04] Processing business logic for job abc-222
[2026-01-08 10:00:06] Business logic completed for job abc-222
[2026-01-08 10:00:06] Job abc-222 status updated to Completed
[2026-01-08 10:00:06] Job abc-222 completed successfully
```

#### Job 3: "API Sync Job" (with failure and retry)

**First Attempt:**
```
[2026-01-08 10:00:04] Processing job abc-333: API Sync Job
[2026-01-08 10:00:04] Job abc-333 status updated to InProgress
[2026-01-08 10:00:04] Processing business logic for job abc-333
[2026-01-08 10:00:05] Error processing job abc-333: Connection timeout
[2026-01-08 10:00:05] Job abc-333 marked as failed
```

**Retry Attempt 1 (after 5 seconds):**
```
[2026-01-08 10:00:10] Retry 1/3 for job abc-333
[2026-01-08 10:00:10] Processing job abc-333: API Sync Job
[2026-01-08 10:00:10] Job abc-333 status updated to InProgress
[2026-01-08 10:00:10] Processing business logic for job abc-333
[2026-01-08 10:00:11] Error processing job abc-333: Connection timeout
```

**Retry Attempt 2 (after 10 seconds - exponential backoff):**
```
[2026-01-08 10:00:20] Retry 2/3 for job abc-333
[2026-01-08 10:00:20] Processing job abc-333: API Sync Job
[2026-01-08 10:00:20] Job abc-333 status updated to InProgress
[2026-01-08 10:00:20] Processing business logic for job abc-333
[2026-01-08 10:00:22] Business logic completed for job abc-333
[2026-01-08 10:00:22] Job abc-333 status updated to Completed
[2026-01-08 10:00:22] Job abc-333 completed successfully
```

### 5. Orchestration Completion

Once all jobs are processed, the orchestrator aggregates results and logs a summary.

```
[2026-01-08 10:00:23] Job processing orchestration completed
[2026-01-08 10:00:23] Success: 3, Failed: 0
[2026-01-08 10:00:23] Next timer schedule at: 2026-01-08 10:05:00
```

## Job Status Flow

```
┌─────────────┐
│  InQueued   │  (Initial state - job waiting to be processed)
└──────┬──────┘
       │
       ▼ (Processing starts)
┌─────────────┐
│ InProgress  │  (Job is being processed)
└──────┬──────┘
       │
       ├─────────────────────┐
       ▼                     ▼
┌─────────────┐       ┌─────────────┐
│  Completed  │       │   Failed    │
└─────────────┘       └─────────────┘
  (Success)           (Error occurred, 
                       retries exhausted)
```

## Sample Job Records

### Before Processing

| ID | Name | Status | Start Time | End Time | Error Message |
|----|------|--------|------------|----------|---------------|
| abc-111 | Data Import Job | InQueued | null | null | null |
| abc-222 | Report Generation | InQueued | null | null | null |
| abc-333 | API Sync Job | InQueued | null | null | null |

### After Processing

| ID | Name | Status | Start Time | End Time | Error Message |
|----|------|--------|------------|----------|---------------|
| abc-111 | Data Import Job | Completed | 2026-01-08 10:00:04 | 2026-01-08 10:00:06 | null |
| abc-222 | Report Generation | Completed | 2026-01-08 10:00:04 | 2026-01-08 10:00:06 | null |
| abc-333 | API Sync Job | Completed | 2026-01-08 10:00:20 | 2026-01-08 10:00:22 | null |

### Example of Failed Job (after all retries)

| ID | Name | Status | Start Time | End Time | Error Message |
|----|------|--------|------------|----------|---------------|
| abc-444 | Broken Job | Failed | 2026-01-08 10:00:04 | 2026-01-08 10:00:40 | Error: Invalid data format |

## Configuration Examples

### Change Schedule to Every 10 Minutes

In `local.settings.json`:
```json
{
  "Values": {
    "SCHEDULE_EXPRESSION": "0 */10 * * * *"
  }
}
```

### Change Schedule to Run Once Per Hour

```json
{
  "Values": {
    "SCHEDULE_EXPRESSION": "0 0 * * * *"
  }
}
```

### Change Schedule to Run Daily at 9 AM

```json
{
  "Values": {
    "SCHEDULE_EXPRESSION": "0 0 9 * * *"
  }
}
```

## Error Handling Examples

### Transient Errors (Handled by Retry)

These errors are automatically retried:
- Network timeouts
- Temporary API unavailability
- Database connection issues
- Rate limiting

**Retry Strategy:**
- Max attempts: 3
- Initial delay: 5 seconds
- Backoff: Exponential (5s, 10s, 20s)

### Permanent Errors (No Retry)

These errors cause immediate failure:
- Invalid job data (missing required fields)
- Authorization failures
- Business logic validation errors

### Example Log Output for Failed Job

```
[2026-01-08 10:00:04] Processing job xyz-999: Invalid Job
[2026-01-08 10:00:04] Job xyz-999 status updated to InProgress
[2026-01-08 10:00:04] Error processing job xyz-999: Job name is required
[2026-01-08 10:00:04] Job xyz-999 marked as failed
[2026-01-08 10:00:05] Retry 1/3 for job xyz-999
[2026-01-08 10:00:05] Error processing job xyz-999: Job name is required
[2026-01-08 10:00:10] Retry 2/3 for job xyz-999
[2026-01-08 10:00:10] Error processing job xyz-999: Job name is required
[2026-01-08 10:00:20] Retry 3/3 for job xyz-999
[2026-01-08 10:00:20] Error processing job xyz-999: Job name is required
[2026-01-08 10:00:20] Job xyz-999 failed after 3 retries
[2026-01-08 10:00:20] Job xyz-999 status updated to Failed with error: Job name is required
```

## Performance Characteristics

### Parallel Processing

The orchestrator processes jobs **in parallel** for better performance:
- **Maximum concurrent activities**: 10 (configurable in host.json)
- **Batch size**: All InQueued jobs in one orchestration
- **Processing time**: 2-5 seconds per job (depends on business logic)

### Example Performance Metrics

With 100 jobs:
- **Sequential processing**: ~5 minutes (3 seconds per job)
- **Parallel processing (10 concurrent)**: ~30 seconds (10 jobs at a time)

### Scalability

The solution scales automatically:
- Azure Functions scales out based on load
- Durable Functions maintains state across scale events
- Multiple orchestration instances can run simultaneously

## Monitoring and Observability

### Application Insights Queries

**Count successful jobs:**
```kusto
traces
| where message contains "completed successfully"
| summarize count() by bin(timestamp, 5m)
```

**Find failed jobs:**
```kusto
traces
| where severityLevel >= 3 // Error level
| where message contains "job"
| project timestamp, message, severityLevel
```

**Track orchestration duration:**
```kusto
requests
| where name == "JobProcessingOrchestrator"
| summarize avg(duration), max(duration), min(duration)
```

## Customization Guide

### Adding Custom Business Logic

Edit `ProcessJobActivity.cs`:

```csharp
private async Task<string> PerformBusinessLogic(Job job)
{
    // Example: Call external API
    using var client = new HttpClient();
    var response = await client.PostAsJsonAsync(
        "https://api.example.com/process", 
        job);
    
    // Example: Transform data
    var transformedData = TransformJobData(job);
    
    // Example: Save to database
    await SaveToDatabase(transformedData);
    
    return $"Processed: {job.Name}";
}
```

### Adding Custom Job Properties

Edit `Models/Job.cs`:

```csharp
public class Job
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public JobStatus Status { get; set; }
    
    // Add custom properties
    public string? CustomField1 { get; set; }
    public int Priority { get; set; }
    public string? AssignedTo { get; set; }
}
```

## Testing Locally

### 1. Start Storage Emulator

```bash
azurite --silent --location c:\azurite
```

### 2. Update Test Data

Modify `DataverseService.cs` to return test jobs:

```csharp
public async Task<List<Job>> GetJobsByStatusAsync(JobStatus status)
{
    // Return test data for local testing
    if (status == JobStatus.InQueued)
    {
        return new List<Job>
        {
            new Job 
            { 
                Id = Guid.NewGuid(), 
                Name = "Test Job 1", 
                Status = JobStatus.InQueued,
                Description = "Test job for demo"
            },
            new Job 
            { 
                Id = Guid.NewGuid(), 
                Name = "Test Job 2", 
                Status = JobStatus.InQueued,
                Description = "Another test job"
            }
        };
    }
    return new List<Job>();
}
```

### 3. Run the Function

```bash
cd DurableJobProcessor
func start
```

### 4. Watch the Logs

The console will show real-time logs as the timer triggers and processes jobs.

## Conclusion

This timer-triggered durable orchestrator provides a robust, scalable solution for processing jobs from Dataverse on a scheduled basis. Key features include:

✅ **Reliable**: Durable Functions ensures workflow completion even during failures  
✅ **Scalable**: Parallel processing and auto-scaling support high throughput  
✅ **Resilient**: Automatic retry with exponential backoff handles transient errors  
✅ **Observable**: Comprehensive logging and Application Insights integration  
✅ **Maintainable**: Clean architecture with separation of concerns  
✅ **Flexible**: Easy to customize schedule, retry policies, and business logic
