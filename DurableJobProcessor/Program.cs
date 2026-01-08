using DurableJobProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Add Application Insights telemetry
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configure logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Register Dataverse service
        // Use mock service for local testing, real service for production
        var useMockService = Environment.GetEnvironmentVariable("USE_MOCK_SERVICE");
        
        // Default to mock service if not specified (for local testing)
        if (useMockService == null || useMockService.ToLower() == "true")
        {
            // Use mock service for local testing without Dataverse
            services.AddSingleton<IDataverseService, MockDataverseService>();
        }
        else
        {
            // Use real Dataverse service for production
            var connectionString = Environment.GetEnvironmentVariable("DataverseConnectionString") 
                ?? "AuthType=OAuth;Url=https://your-org.crm.dynamics.com;ClientId=your-client-id;ClientSecret=your-client-secret;";
            
            services.AddSingleton<IDataverseService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<DataverseService>>();
                return new DataverseService(logger, connectionString);
            });
        }

        // Add other services as needed
        // services.AddSingleton<IOtherService, OtherService>();
    })
    .Build();

host.Run();
