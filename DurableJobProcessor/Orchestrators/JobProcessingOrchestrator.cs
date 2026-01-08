using DurableJobProcessor.Activities;
using DurableJobProcessor.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableJobProcessor.Orchestrators;

/// <summary>
/// Orchestrator function that coordinates the processing of queued jobs
/// This function is called by the timer trigger and manages the workflow
/// </summary>
public class JobProcessingOrchestrator
{
    private readonly ILogger<JobProcessingOrchestrator> _logger;

    public JobProcessingOrchestrator(ILogger<JobProcessingOrchestrator> logger)
    {
        _logger = logger;
    }

    [Function(nameof(JobProcessingOrchestrator))]
    public async Task RunAsync([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(nameof(JobProcessingOrchestrator));
        
        try
        {
            logger.LogInformation("Job processing orchestration started at {Time}", context.CurrentUtcDateTime);

            // Step 1: Get all jobs with InQueued status from Dataverse
            var jobs = await context.CallActivityAsync<List<Job>>(
                nameof(GetQueuedJobsActivity));

            logger.LogInformation("Retrieved {Count} jobs to process", jobs.Count);

            if (jobs.Count == 0)
            {
                logger.LogInformation("No jobs to process, orchestration completed");
                return;
            }

            // Step 2: Process each job
            // Using parallel processing with fan-out/fan-in pattern for better performance
            // Alternatively, can process sequentially if order matters or to limit concurrency
            var processingTasks = new List<Task<ProcessingResult>>();

            foreach (var job in jobs)
            {
                logger.LogInformation("Scheduling job {JobId} for processing", job.Id);
                
                // Create a task to process each job with error handling
                var task = ProcessJobWithRetryAsync(context, job, logger);
                processingTasks.Add(task);
            }

            // Wait for all jobs to complete (fan-in)
            var results = await Task.WhenAll(processingTasks);

            // Step 3: Log summary results
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            logger.LogInformation(
                "Job processing orchestration completed. Success: {SuccessCount}, Failed: {FailedCount}", 
                successCount, 
                failureCount);

            foreach (var result in results.Where(r => !r.Success))
            {
                logger.LogWarning(
                    "Job {JobId} failed: {ErrorMessage}", 
                    result.JobId, 
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error in job processing orchestration: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Processes a single job with retry logic and error handling
    /// </summary>
    private async Task<ProcessingResult> ProcessJobWithRetryAsync(
        TaskOrchestrationContext context, 
        Job job, 
        ILogger logger)
    {
        const int maxRetries = 3;
        var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: maxRetries,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: 2.0));

        try
        {
            logger.LogInformation("Processing job {JobId} with retry policy", job.Id);

            // Call the ProcessJobActivity with retry logic
            var success = await context.CallActivityAsync<bool>(
                nameof(ProcessJobActivity),
                job,
                retryOptions);

            return new ProcessingResult
            {
                JobId = job.Id,
                Success = success,
                ErrorMessage = null
            };
        }
        catch (TaskFailedException ex)
        {
            // Job failed even after retries
            logger.LogError(
                ex, 
                "Job {JobId} failed after {MaxRetries} retries: {ErrorMessage}", 
                job.Id, 
                maxRetries, 
                ex.Message);

            // Ensure the job status is marked as Failed in Dataverse
            try
            {
                await context.CallActivityAsync(
                    nameof(UpdateJobStatusActivity),
                    new UpdateJobStatusRequest(job.Id, JobStatus.Failed, ex.Message));
            }
            catch (Exception updateEx)
            {
                logger.LogError(
                    updateEx, 
                    "Failed to update job {JobId} status to Failed: {ErrorMessage}", 
                    job.Id, 
                    updateEx.Message);
                
                // Include status update failure in result
                return new ProcessingResult
                {
                    JobId = job.Id,
                    Success = false,
                    ErrorMessage = $"{ex.Message} (Status update also failed: {updateEx.Message})"
                };
            }

            return new ProcessingResult
            {
                JobId = job.Id,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            // Unexpected error
            logger.LogError(
                ex, 
                "Unexpected error processing job {JobId}: {ErrorMessage}", 
                job.Id, 
                ex.Message);

            return new ProcessingResult
            {
                JobId = job.Id,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Result of processing a single job
/// </summary>
public class ProcessingResult
{
    public Guid JobId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
