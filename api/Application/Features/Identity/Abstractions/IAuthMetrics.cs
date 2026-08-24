namespace Application.Features.Identity.Abstractions;

/// <summary>Production counters for authentication abuse and session health.</summary>
public interface IAuthMetrics
{
    void LoginSucceeded();

    void LoginFailed();

    void RefreshSucceeded();

    void RefreshFailed();

    void RefreshReuseDetected();

    void RateLimited();

    void MfaChallengeIssued();

    void MfaSucceeded();

    void MfaFailed();
}
