using DurableJobProcessor.Models;

namespace DurableJobProcessor.Services;

/// <summary>
/// Interface for interacting with Dataverse for job operations
/// </summary>
public interface IDataverseService
{
    /// <summary>
    /// Retrieves all jobs with the specified status from Dataverse
    /// </summary>
    /// <param name="status">The job status to filter by</param>
    /// <returns>A list of jobs matching the status</returns>
    Task<List<Job>> GetJobsByStatusAsync(JobStatus status);

    /// <summary>
    /// Updates the status of a job in Dataverse
    /// </summary>
    /// <param name="jobId">The ID of the job to update</param>
    /// <param name="status">The new status</param>
    /// <param name="errorMessage">Optional error message if the job failed</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateJobStatusAsync(Guid jobId, JobStatus status, string? errorMessage = null);

    /// <summary>
    /// Gets a specific job by ID from Dataverse
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <returns>The job if found, null otherwise</returns>
    Task<Job?> GetJobByIdAsync(Guid jobId);
}
