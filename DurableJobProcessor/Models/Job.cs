namespace DurableJobProcessor.Models;

/// <summary>
/// Represents job status values in Dataverse
/// </summary>
public enum JobStatus
{
    InQueued = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>
/// Represents a job entity from Dataverse
/// </summary>
public class Job
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public JobStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
