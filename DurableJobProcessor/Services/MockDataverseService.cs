using DurableJobProcessor.Models;
using DurableJobProcessor.Services;
using Microsoft.Extensions.Logging;

namespace DurableJobProcessor.Services;

/// <summary>
/// Mock implementation of Dataverse service for testing and demonstration purposes
/// This version returns test data to allow local testing without a Dataverse instance
/// </summary>
public class MockDataverseService : IDataverseService
{
    private readonly ILogger<MockDataverseService> _logger;
    private readonly Dictionary<Guid, Job> _jobStore;
    private int _queryCount = 0;

    public MockDataverseService(ILogger<MockDataverseService> logger)
    {
        _logger = logger;
        _jobStore = new Dictionary<Guid, Job>();
        InitializeMockJobs();
    }

    private void InitializeMockJobs()
    {
        // Create some test jobs
        var testJobs = new List<Job>
        {
            new Job
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Data Import Job",
                Status = JobStatus.InQueued,
                Description = "Import customer data from external system",
                RetryCount = 0
            },
            new Job
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Report Generation",
                Status = JobStatus.InQueued,
                Description = "Generate monthly sales report",
                RetryCount = 0
            },
            new Job
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "API Sync Job",
                Status = JobStatus.InQueued,
                Description = "Synchronize data with external API",
                RetryCount = 0
            }
        };

        foreach (var job in testJobs)
        {
            _jobStore[job.Id] = job;
        }

        _logger.LogInformation("Initialized mock job store with {Count} test jobs", testJobs.Count);
    }

    public async Task<List<Job>> GetJobsByStatusAsync(JobStatus status)
    {
        try
        {
            _logger.LogInformation("MOCK: Querying for jobs with status: {Status}", status);

            await Task.Delay(100); // Simulate API call delay

            // Return jobs only on first query to simulate one-time processing
            // Using instance-based counter instead of static for thread safety
            if (status == JobStatus.InQueued && _queryCount == 0)
            {
                _queryCount++;
                var queuedJobs = _jobStore.Values
                    .Where(j => j.Status == JobStatus.InQueued)
                    .ToList();

                _logger.LogInformation("MOCK: Found {Count} jobs with status {Status}", queuedJobs.Count, status);
                return queuedJobs;
            }

            _logger.LogInformation("MOCK: No jobs found with status {Status}", status);
            return new List<Job>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Error querying for jobs with status {Status}", status);
            throw;
        }
    }

    public async Task<bool> UpdateJobStatusAsync(Guid jobId, JobStatus status, string? errorMessage = null)
    {
        try
        {
            _logger.LogInformation("MOCK: Updating job {JobId} status to {Status}", jobId, status);

            await Task.Delay(50); // Simulate API call delay

            if (_jobStore.TryGetValue(jobId, out var job))
            {
                job.Status = status;
                job.ErrorMessage = errorMessage;

                if (status == JobStatus.InProgress && job.StartTime == null)
                {
                    job.StartTime = DateTime.UtcNow;
                }
                else if (status == JobStatus.Completed || status == JobStatus.Failed)
                {
                    job.EndTime = DateTime.UtcNow;
                }

                _logger.LogInformation(
                    "MOCK: Successfully updated job {JobId} to status {Status}", 
                    jobId, 
                    status);

                // Log job details for demo purposes
                _logger.LogInformation(
                    "MOCK: Job Details - Name: {Name}, StartTime: {StartTime}, EndTime: {EndTime}, Error: {Error}",
                    job.Name,
                    job.StartTime,
                    job.EndTime,
                    job.ErrorMessage ?? "None");

                return true;
            }

            _logger.LogWarning("MOCK: Job {JobId} not found in mock store", jobId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Error updating job {JobId} status to {Status}", jobId, status);
            throw;
        }
    }

    public async Task<Job?> GetJobByIdAsync(Guid jobId)
    {
        try
        {
            _logger.LogInformation("MOCK: Retrieving job {JobId}", jobId);

            await Task.Delay(50); // Simulate API call delay

            if (_jobStore.TryGetValue(jobId, out var job))
            {
                _logger.LogInformation("MOCK: Found job {JobId}: {JobName}", jobId, job.Name);
                return job;
            }

            _logger.LogWarning("MOCK: Job {JobId} not found", jobId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Error retrieving job {JobId}", jobId);
            throw;
        }
    }

    /// <summary>
    /// Helper method to reset the mock data for testing
    /// </summary>
    public void ResetMockData()
    {
        _jobStore.Clear();
        _queryCount = 0;
        InitializeMockJobs();
        _logger.LogInformation("MOCK: Reset mock data store");
    }

    /// <summary>
    /// Helper method to get all jobs (for testing purposes)
    /// </summary>
    public List<Job> GetAllJobs()
    {
        return _jobStore.Values.ToList();
    }
}
