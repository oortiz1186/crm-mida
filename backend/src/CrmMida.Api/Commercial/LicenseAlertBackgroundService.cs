namespace CrmMida.Api.Commercial;

public sealed class LicenseAlertBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<LicenseAlertBackgroundService> logger) : BackgroundService
{
    private DateOnly? lastRunDate;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!configuration.GetValue("LicenseAlerts:Enabled", false)) continue;

            var timeZoneId = configuration["LicenseAlerts:TimeZone"] ?? "America/Mexico_City";
            TimeZoneInfo timeZone;
            try { timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
            catch { timeZone = TimeZoneInfo.Utc; }

            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var scheduledHour = Math.Clamp(configuration.GetValue("LicenseAlerts:Hour", 8), 0, 23);
            var localDate = DateOnly.FromDateTime(localNow);
            if (localNow.Hour < scheduledHour || lastRunDate == localDate) continue;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<LicenseAlertProcessor>();
                var result = await processor.ProcessAsync(configuration.GetValue("LicenseAlerts:Days", 90), stoppingToken);
                lastRunDate = localDate;
                logger.LogInformation(
                    "Job de licencias finalizado. Evaluadas {Evaluated}, creadas {Created}, notificadas {Notified}, fallidas {Failures}",
                    result.Evaluated, result.Created, result.Notified, result.NotificationFailures);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falló el job diario de alertas de licencias.");
            }
        }
    }
}
