using DurableJobProcessor.Models;
using DurableJobProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableJobProcessor.Activities;

/// <summary>
/// Activity function to update job status in Dataverse
/// This is a separate activity to allow for retry logic on status updates
/// </summary>
public class UpdateJobStatusActivity
{
    private readonly ILogger<UpdateJobStatusActivity> _logger;
    private readonly IDataverseService _dataverseService;

    public UpdateJobStatusActivity(ILogger<UpdateJobStatusActivity> logger, IDataverseService dataverseService)
    {
        _logger = logger;
        _dataverseService = dataverseService;
    }

    [Function(nameof(UpdateJobStatusActivity))]
    public async Task<bool> RunAsync([ActivityTrigger] UpdateJobStatusRequest request, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(UpdateJobStatusActivity));

        try
        {
            logger.LogInformation(
                "Updating job {JobId} status to {Status}", 
                request.JobId, 
                request.Status);

            var result = await _dataverseService.UpdateJobStatusAsync(
                request.JobId, 
                request.Status, 
                request.ErrorMessage);

            if (result)
            {
                logger.LogInformation(
                    "Successfully updated job {JobId} status to {Status}", 
                    request.JobId, 
                    request.Status);
            }
            else
            {
                logger.LogWarning(
                    "Failed to update job {JobId} status to {Status}", 
                    request.JobId, 
                    request.Status);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, 
                "Error updating job {JobId} status to {Status}: {ErrorMessage}", 
                request.JobId, 
                request.Status, 
                ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Request object for updating job status
/// </summary>
public record UpdateJobStatusRequest(Guid JobId, JobStatus Status, string? ErrorMessage = null);
