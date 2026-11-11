using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Services.Background;

/// <summary>Runs on startup and every minute; each pass uses its own scoped DbContext.</summary>
public sealed class AttendanceGenerationBackgroundService(
    IServiceScopeFactory scopes,
    ILogger<AttendanceGenerationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var result = await scope.ServiceProvider.GetRequiredService<IAttendanceService>().EnsureTodaySessionsAsync();
                if (!result.Success) logger.LogWarning("Automatic attendance generation failed: {Message}", result.Message);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Automatic attendance generation failed; will retry next minute.");
            }
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
