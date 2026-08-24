using Api.Configuration;
using Application.Features.Identity.Abstractions;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Api.Extensions;

internal static class OpenTelemetryExtensions
{
    public static IServiceCollection AddApiOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        var otlpEndpoint =
            FirstNonEmpty(
                options.OtlpEndpoint,
                configuration["OTEL_EXPORTER_OTLP_ENDPOINT"],
                Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

        var exportEnabled = options.Enabled || !string.IsNullOrWhiteSpace(otlpEndpoint);
        if (!exportEnabled)
        {
            return services;
        }

        var endpoint = string.IsNullOrWhiteSpace(otlpEndpoint)
            ? new Uri("http://localhost:4317")
            : new Uri(otlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: environment.ApplicationName,
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(instrumentation =>
                {
                    instrumentation.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation(instrumentation =>
                {
                    instrumentation.RecordException = true;
                })
                .AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = endpoint;
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(AuthMetricNames.MeterName)
                .AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = endpoint;
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithLogging(logging => logging
                .AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = endpoint;
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                }));

        return services;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
