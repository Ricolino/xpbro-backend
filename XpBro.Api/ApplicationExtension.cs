using Serilog;

namespace XpBro.Api;

public static class ApplicationExtension
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                //.WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs/log-.txt"),
                //              fileSizeLimitBytes: 2 * 1024 * 1024,
                //              rollingInterval: RollingInterval.Day,
                //              rollOnFileSizeLimit: true,
                //              retainedFileCountLimit: 10)
                .WriteTo.Console();

                //NuGet Serilog.Sinks.ApplicationInsights
                //.WriteTo.ApplicationInsights(
                //    context.Configuration["ApplicationInsights:ConnectionString"],
                //    TelemetryConverter.Traces);
        });
    }
}
