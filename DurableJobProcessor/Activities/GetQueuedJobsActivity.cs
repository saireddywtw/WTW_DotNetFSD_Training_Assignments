using DurableJobProcessor.Models;
using DurableJobProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace DurableJobProcessor.Activities;

/// <summary>
/// Activity function to query Dataverse for jobs with InQueued status
/// </summary>
public class GetQueuedJobsActivity
{
    private readonly ILogger<GetQueuedJobsActivity> _logger;
    private readonly IDataverseService _dataverseService;

    public GetQueuedJobsActivity(ILogger<GetQueuedJobsActivity> logger, IDataverseService dataverseService)
    {
        _logger = logger;
        _dataverseService = dataverseService;
    }

    [Function(nameof(GetQueuedJobsActivity))]
    public async Task<List<Job>> RunAsync([ActivityTrigger] FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(GetQueuedJobsActivity));

        try
        {
            logger.LogInformation("Querying Dataverse for jobs with InQueued status");

            var jobs = await _dataverseService.GetJobsByStatusAsync(JobStatus.InQueued);

            logger.LogInformation("Found {Count} jobs with InQueued status", jobs.Count);

            return jobs;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying Dataverse for InQueued jobs: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}
