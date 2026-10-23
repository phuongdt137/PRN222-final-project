using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Services.Background;

public class ScoreAutoLockBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScoreAutoLockBackgroundService> _logger;

    public ScoreAutoLockBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ScoreAutoLockBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScoreAutoLockBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var scoreService = scope.ServiceProvider.GetRequiredService<IScoreService>();

                var now = DateTime.UtcNow;

                // Tìm các bài kiểm tra đã quá hạn 7 ngày
                var expiredAssessmentIds = await context.Assessments
                    .AsNoTracking()
                    .Where(a => a.ConductedAt.HasValue && now >= a.ConductedAt.Value.AddDays(7))
                    .Select(a => a.Id)
                    .ToListAsync(stoppingToken);

                foreach (var assId in expiredAssessmentIds)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    // Trigger logic kiểm tra & auto-fill điểm 0 cho học sinh trống
                    await scoreService.GetScoresForAssessmentAsync(assId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong tiến trình tự động khóa điểm nền.");
            }

            // Chờ 2 phút trước khi quét lại
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ScoreAutoLockBackgroundService stopped.");
    }
}
