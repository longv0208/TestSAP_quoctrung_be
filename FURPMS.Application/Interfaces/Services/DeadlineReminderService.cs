using FURPMS.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FURPMS.Infrastructure.Services;

public class DeadlineReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeadlineReminderService> _logger;

    public DeadlineReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeadlineReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<IDeadlineReminderScanner>();
            await scanner.ScanAsync(ct);
            _logger.LogInformation("DeadlineReminderService scan complete");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "DeadlineReminderService scan failed");
        }
    }
}
