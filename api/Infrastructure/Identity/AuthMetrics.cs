using System.Diagnostics.Metrics;
using Application.Features.Identity.Abstractions;

namespace Infrastructure.Identity;

/// <summary>
/// Emits System.Diagnostics.Metrics counters under meter name
/// <see cref="AuthMetricNames.MeterName"/> for scrape by OpenTelemetry agents.
/// </summary>
internal sealed class AuthMetrics : IAuthMetrics
{
    private readonly Counter<long> _loginSucceeded;
    private readonly Counter<long> _loginFailed;
    private readonly Counter<long> _refreshSucceeded;
    private readonly Counter<long> _refreshFailed;
    private readonly Counter<long> _refreshReuseDetected;
    private readonly Counter<long> _rateLimited;
    private readonly Counter<long> _mfaChallengeIssued;
    private readonly Counter<long> _mfaSucceeded;
    private readonly Counter<long> _mfaFailed;

    public AuthMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(AuthMetricNames.MeterName);
        _loginSucceeded = meter.CreateCounter<long>(
            "auth.login.succeeded",
            unit: "{attempt}",
            description: "Successful password logins that issued tokens (no MFA step).");
        _loginFailed = meter.CreateCounter<long>(
            "auth.login.failed",
            unit: "{attempt}",
            description: "Failed password logins (invalid credentials or lockout).");
        _refreshSucceeded = meter.CreateCounter<long>(
            "auth.refresh.succeeded",
            unit: "{attempt}",
            description: "Successful refresh-token rotations.");
        _refreshFailed = meter.CreateCounter<long>(
            "auth.refresh.failed",
            unit: "{attempt}",
            description: "Failed refresh attempts (invalid, expired, locked, or race).");
        _refreshReuseDetected = meter.CreateCounter<long>(
            "auth.refresh.reuse_detected",
            unit: "{event}",
            description: "Refresh-token reuse/theft detections that revoke a family.");
        _rateLimited = meter.CreateCounter<long>(
            "auth.rate_limited",
            unit: "{request}",
            description: "Authentication endpoints rejected by rate limiting.");
        _mfaChallengeIssued = meter.CreateCounter<long>(
            "auth.mfa.challenge_issued",
            unit: "{attempt}",
            description: "Password logins that require a TOTP second factor.");
        _mfaSucceeded = meter.CreateCounter<long>(
            "auth.mfa.succeeded",
            unit: "{attempt}",
            description: "Successful TOTP verifications that issued tokens.");
        _mfaFailed = meter.CreateCounter<long>(
            "auth.mfa.failed",
            unit: "{attempt}",
            description: "Failed TOTP verifications.");
    }

    public void LoginSucceeded() => _loginSucceeded.Add(1);

    public void LoginFailed() => _loginFailed.Add(1);

    public void RefreshSucceeded() => _refreshSucceeded.Add(1);

    public void RefreshFailed() => _refreshFailed.Add(1);

    public void RefreshReuseDetected() => _refreshReuseDetected.Add(1);

    public void RateLimited() => _rateLimited.Add(1);

    public void MfaChallengeIssued() => _mfaChallengeIssued.Add(1);

    public void MfaSucceeded() => _mfaSucceeded.Add(1);

    public void MfaFailed() => _mfaFailed.Add(1);
}
