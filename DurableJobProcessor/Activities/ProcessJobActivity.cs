using DurableJobProcessor.Models;
using DurableJobProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableJobProcessor.Activities;

/// <summary>
/// Activity function that processes an individual job
/// This performs the main business logic for job processing
/// </summary>
public class ProcessJobActivity
{
    private readonly ILogger<ProcessJobActivity> _logger;
    private readonly IDataverseService _dataverseService;

    public ProcessJobActivity(ILogger<ProcessJobActivity> logger, IDataverseService dataverseService)
    {
        _logger = logger;
        _dataverseService = dataverseService;
    }

    [Function(nameof(ProcessJobActivity))]
    public async Task<bool> RunAsync([ActivityTrigger] Job job, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(ProcessJobActivity));
        
        try
        {
            logger.LogInformation("Starting to process job {JobId}: {JobName}", job.Id, job.Name);

            // Update job status to InProgress
            await _dataverseService.UpdateJobStatusAsync(job.Id, JobStatus.InProgress);
            logger.LogInformation("Job {JobId} status updated to InProgress", job.Id);

            // Simulate the main business work
            // In a real scenario, this would contain the actual job processing logic
            // Examples: data transformation, API calls, calculations, etc.
            await Task.Delay(TimeSpan.FromSeconds(2)); // Simulate processing time

            // Simulate some business logic
            logger.LogInformation("Processing business logic for job {JobId}", job.Id);
            
            // Example: validate job data
            if (string.IsNullOrEmpty(job.Name))
            {
                throw new InvalidOperationException("Job name is required");
            }

            // Example: perform calculations or transformations
            var result = PerformBusinessLogic(job);
            logger.LogInformation("Business logic completed for job {JobId} with result: {Result}", job.Id, result);

            // Update job status to Completed
            await _dataverseService.UpdateJobStatusAsync(job.Id, JobStatus.Completed);
            logger.LogInformation("Job {JobId} completed successfully", job.Id);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing job {JobId}: {ErrorMessage}", job.Id, ex.Message);
            
            // Update job status to Failed with error message
            await _dataverseService.UpdateJobStatusAsync(job.Id, JobStatus.Failed, ex.Message);
            logger.LogWarning("Job {JobId} marked as failed", job.Id);

            // Re-throw to let orchestrator handle retry logic
            throw;
        }
    }

    /// <summary>
    /// Example business logic processing method
    /// Replace with actual business logic as needed
    /// </summary>
    private string PerformBusinessLogic(Job job)
    {
        // Placeholder for actual business logic
        // This could be:
        // - Data transformation
        // - API calls to external systems
        // - Complex calculations
        // - File processing
        // - etc.
        
        _logger.LogInformation("Executing business logic for job: {JobName}", job.Name);
        
        // Example logic
        return $"Processed: {job.Name} at {DateTime.UtcNow}";
    }
}
