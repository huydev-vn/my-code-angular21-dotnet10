using Application.Common.Time;
using Application.Features.Identity.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

/// <summary>
/// Periodically deletes revoked or expired refresh tokens past the retention window
/// so the table does not grow without bound.
/// </summary>
internal sealed class RefreshTokenCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<IdentitySettings> identityOptions,
    ILogger<RefreshTokenCleanupHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay the first run so startup seeding and migrations settle.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Refresh-token cleanup cycle failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        var settings = identityOptions.Value;
        var retentionDays = settings.RefreshTokenRetentionDays > 0
            ? settings.RefreshTokenRetentionDays
            : IdentitySettings.DefaultRefreshTokenRetentionDays;
        var batchSize = settings.RefreshTokenCleanupBatchSize > 0
            ? settings.RefreshTokenCleanupBatchSize
            : IdentitySettings.DefaultRefreshTokenCleanupBatchSize;

        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var olderThan = clock.UtcNow.AddDays(-retentionDays);

        var deleted = await store.PurgeStaleAsync(olderThan, batchSize, cancellationToken);
        if (deleted > 0)
        {
            logger.LogInformation(
                "Purged {DeletedCount} stale refresh tokens older than {OlderThan:u}.",
                deleted,
                olderThan);
        }
    }
}
