namespace FURPMS.Application.Interfaces.Services;

public interface IDeadlineReminderScanner
{
    Task ScanAsync(CancellationToken ct = default);
}
