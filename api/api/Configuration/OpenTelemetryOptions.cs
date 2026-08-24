namespace Api.Configuration;

/// <summary>
/// OpenTelemetry export. Metrics always register locally via <c>IMeterFactory</c>;
/// OTLP export is enabled when an endpoint is configured.
/// </summary>
internal sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// When true, registers OTLP export. Also enabled automatically when
    /// <see cref="OtlpEndpoint"/> or <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>OTLP endpoint, for example <c>http://localhost:4317</c>.</summary>
    public string? OtlpEndpoint { get; init; }
}
