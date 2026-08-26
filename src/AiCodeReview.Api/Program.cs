using System.Diagnostics;
using AiCodeReview.Api;
using AiCodeReview.Api.Extensions;
using AiCodeReview.Api.Middleware;
using AiCodeReview.Application;
using AiCodeReview.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// A bootstrap logger captures failures that happen before configuration is
// read; without it, a bad appsettings file fails silently.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", ApiConstants.ApplicationName));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??=
                $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddApplicationHealthChecks();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Correlation runs first so every later log line — including the one the
    // exception handler writes — carries the id.
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = static (httpContext, _, exception) => exception is not null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
                ? LogEventLevel.Error
                : IsHealthProbe(httpContext)
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information;
    });

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    app.MapHealthEndpoints();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "{Application} terminated unexpectedly", ApiConstants.ApplicationName);
}
finally
{
    Log.CloseAndFlush();
}

static bool IsHealthProbe(HttpContext context) =>
    context.Request.Path.StartsWithSegments(ApiConstants.HealthChecks.LivePath, StringComparison.OrdinalIgnoreCase);
