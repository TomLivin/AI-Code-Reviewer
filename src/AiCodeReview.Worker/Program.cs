using AiCodeReview.Application;
using AiCodeReview.Infrastructure;
using AiCodeReview.Worker;
using AiCodeReview.Worker.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", WorkerConstants.ApplicationName));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services
        .AddOptions<WorkerOptions>()
        .Bind(builder.Configuration.GetSection(WorkerOptions.SectionName))
        .Validate(
            static options => options.HeartbeatSeconds is > 0 and <= 3600,
            $"{WorkerOptions.SectionName}:{nameof(WorkerOptions.HeartbeatSeconds)} must be between 1 and 3600.")
        .ValidateOnStart();

    builder.Services.AddHostedService<WorkerHeartbeatService>();

    IHost host = builder.Build();
    host.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "{Application} terminated unexpectedly", WorkerConstants.ApplicationName);
}
finally
{
    Log.CloseAndFlush();
}
