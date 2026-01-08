using DurableJobProcessor.Orchestrators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableJobProcessor.Triggers;

/// <summary>
/// Timer-triggered function that starts the job processing orchestration
/// Runs on a schedule defined in local.settings.json (default: every 5 minutes)
/// </summary>
public class JobProcessingTimerTrigger
{
    private readonly ILogger<JobProcessingTimerTrigger> _logger;

    public JobProcessingTimerTrigger(ILogger<JobProcessingTimerTrigger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Timer trigger function that runs on a schedule
    /// Default schedule: "0 */5 * * * *" (every 5 minutes)
    /// Schedule format: {second} {minute} {hour} {day} {month} {day-of-week}
    /// </summary>
    /// <param name="timerInfo">Timer schedule information</param>
    /// <param name="client">Durable task client to start orchestrations</param>
    /// <param name="executionContext">Function execution context</param>
    [Function(nameof(JobProcessingTimerTrigger))]
    public async Task RunAsync(
        [TimerTrigger("%SCHEDULE_EXPRESSION%")] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(JobProcessingTimerTrigger));

        try
        {
            logger.LogInformation(
                "Job processing timer trigger fired at: {Time}", 
                DateTime.UtcNow);

            if (timerInfo.IsPastDue)
            {
                logger.LogWarning(
                    "Timer trigger is running late. Schedule status: Past due");
            }

            // Start a new orchestration instance
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(JobProcessingOrchestrator));

            logger.LogInformation(
                "Started orchestration with ID = '{InstanceId}' at {Time}",
                instanceId,
                DateTime.UtcNow);

            // Optional: Check if previous orchestration is still running
            // This prevents overlapping orchestrations if processing takes longer than the schedule interval
            var status = await client.GetInstanceAsync(instanceId);
            
            if (status != null)
            {
                logger.LogInformation(
                    "Orchestration {InstanceId} status: {RuntimeStatus}",
                    instanceId,
                    status.RuntimeStatus);
            }

            logger.LogInformation(
                "Next timer schedule at: {NextSchedule}",
                timerInfo.ScheduleStatus?.Next);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error in timer trigger: {ErrorMessage}",
                ex.Message);
            throw;
        }
    }
}
