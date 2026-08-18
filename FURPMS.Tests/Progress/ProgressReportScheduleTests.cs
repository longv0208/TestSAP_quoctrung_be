using FURPMS.Application.DTOs.Progress;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;

namespace FURPMS.Tests.Progress;

public class ProgressReportScheduleTests
{
    private static async Task<(ProgressReportService service, ProgressReport report)> SeedAsync()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ContractNumber = "CTR-SCHEDULE",
            TotalAmount = 1_000_000m,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 12, 31),
            OriginalEndDate = new DateOnly(2026, 12, 31),
            Status = "ACTIVE",
            CreatedBy = Guid.NewGuid()
        };
        var report = new ProgressReport
        {
            ContractId = contract.Id,
            ReportRound = 1,
            ReportingPeriodStart = new DateOnly(2026, 8, 1),
            ReportingPeriodEnd = new DateOnly(2026, 9, 30),
            DueDate = new DateOnly(2026, 10, 5),
            CompletedContent = "",
            Status = "DRAFT"
        };
        db.Contracts.Add(contract);
        db.ProgressReports.Add(report);
        await db.SaveChangesAsync();

        var clock = new FakeClock { UtcNow = new DateTime(2026, 8, 19, 3, 0, 0, DateTimeKind.Utc) };
        var service = new ProgressReportService(
            new ContractRepository(db), new ProposalRepository(db), clock,
            new DocumentRepository(db), TestNotifier.Create(db));
        return (service, report);
    }

    [Fact]
    public async Task Schedule_PastDueDate_IsBlocked()
    {
        var (service, report) = await SeedAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ScheduleAsync(report.Id,
            new ScheduleProgressReportRequest { DueDate = "2026-08-18" }));

        Assert.Contains("quá khứ", ex.Message);
    }

    [Fact]
    public async Task Schedule_DueDateOutsideContract_IsBlocked()
    {
        var (service, report) = await SeedAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ScheduleAsync(report.Id,
            new ScheduleProgressReportRequest { DueDate = "2027-01-01" }));

        Assert.Contains("thời gian hợp đồng", ex.Message);
    }

    [Fact]
    public async Task Schedule_PastMeeting_IsBlocked()
    {
        var (service, report) = await SeedAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ScheduleAsync(report.Id,
            new ScheduleProgressReportRequest { ScheduledMeetingAt = "2026-08-18T10:00:00Z" }));

        Assert.Contains("quá khứ", ex.Message);
    }

    [Fact]
    public async Task Schedule_MeetingBeforeDueDate_IsBlocked()
    {
        var (service, report) = await SeedAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ScheduleAsync(report.Id,
            new ScheduleProgressReportRequest { ScheduledMeetingAt = "2026-10-04T03:00:00Z" }));

        Assert.Contains("trước hạn nộp", ex.Message);
    }

    [Fact]
    public async Task Schedule_ValidDatesWithinContract_Succeeds()
    {
        var (service, report) = await SeedAsync();

        var result = await service.ScheduleAsync(report.Id, new ScheduleProgressReportRequest
        {
            DueDate = "2026-10-10",
            ScheduledMeetingAt = "2026-10-10T03:00:00Z"
        });

        Assert.Equal("2026-10-10", result.DueDate);
        Assert.Equal(new DateTime(2026, 10, 10, 3, 0, 0, DateTimeKind.Utc), result.ScheduledMeetingAt);
    }
}
