using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;

namespace FURPMS.Tests.Progress;

public class ProgressReportSubmissionTests
{
    private static readonly DateOnly Start = new(2026, 1, 1);
    private static readonly DateOnly End = new(2026, 12, 31);

    private static async Task<(FURPMSDbContext db, User pi, Contract contract)> SeedAsync()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = new User
        {
            Id = Guid.NewGuid(), Email = $"pi-{Guid.NewGuid():N}@test.com", FullName = "PI", Status = UserStatus.Active
        };
        var project = new Project
        {
            Id = Guid.NewGuid(), PiUserId = pi.Id, CycleTrackId = 1, OrderId = 1, HostingUnitId = 1,
            ResearchTypeId = 1, TitleVi = "Đề tài", Status = ProjectStatus.InProgress,
            PlannedStartDate = Start, PlannedEndDate = End
        };
        var contract = new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "CTR-PROGRESS",
            TotalAmount = 1_000_000m, StartDate = Start, EndDate = End, OriginalEndDate = End,
            Status = ContractStatus.Active, CreatedBy = pi.Id
        };
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        return (db, pi, contract);
    }

    private static CreateProgressReportRequest Request(string start = "2026-02-01", string end = "2026-03-31") => new()
    {
        ReportingPeriodStart = start, ReportingPeriodEnd = end, CompletedContent = "Đã hoàn thành giai đoạn 1",
        OverallCompletionPct = 50m
    };

    private static ProgressReportService Service(FURPMSDbContext db) =>
        TestServices.ProgressReports(db, new FakeClock { UtcNow = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) });

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task Create_CompletionBoundary_IsAccepted(decimal completion)
    {
        var (db, pi, contract) = await SeedAsync();
        var request = Request();
        request.OverallCompletionPct = completion;

        var result = await Service(db).CreateAsync(contract.Id, request, pi.Id);

        Assert.Equal(completion, result.OverallCompletionPct);
        Assert.Equal(ProgressReportStatus.Draft, result.Status);
        Assert.Equal(1, result.ReportRound);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public async Task Create_CompletionOutsideRange_IsRejected(decimal completion)
    {
        var (db, pi, contract) = await SeedAsync();
        var request = Request();
        request.OverallCompletionPct = completion;

        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).CreateAsync(contract.Id, request, pi.Id));
    }

    [Fact]
    public async Task Create_InvalidPeriodOrderOrOutsideContract_IsRejected()
    {
        var (db, pi, contract) = await SeedAsync();
        var backwards = Request("2026-03-31", "2026-03-01");
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).CreateAsync(contract.Id, backwards, pi.Id));

        var outside = Request("2025-12-01", "2026-01-15");
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).CreateAsync(contract.Id, outside, pi.Id));
    }

    [Fact]
    public async Task Create_ByNonPi_IsForbidden()
    {
        var (db, _, contract) = await SeedAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => Service(db).CreateAsync(contract.Id, Request(), Guid.NewGuid()));
    }

    [Fact]
    public async Task Submit_DraftByPi_SetsSubmittedStatusAndClockTime()
    {
        var (db, pi, contract) = await SeedAsync();
        var draft = await Service(db).CreateAsync(contract.Id, Request(), pi.Id);

        var result = await Service(db).SubmitAsync(draft.Id, pi.Id);

        Assert.Equal(ProgressReportStatus.Submitted, result.Status);
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), result.SubmittedAt);
    }

    [Fact]
    public async Task Submit_LaterRoundBeforeEarlierEvaluation_IsBlocked()
    {
        var (db, pi, contract) = await SeedAsync();
        var first = await Service(db).CreateAsync(contract.Id, Request(), pi.Id);
        await Service(db).SubmitAsync(first.Id, pi.Id);
        var second = new ProgressReport
        {
            ContractId = contract.Id, ReportRound = 2, ReportingPeriodStart = new DateOnly(2026, 4, 1),
            ReportingPeriodEnd = new DateOnly(2026, 6, 30), CompletedContent = "Kỳ 2", Status = ProgressReportStatus.Draft
        };
        db.ProgressReports.Add(second);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).SubmitAsync(second.Id, pi.Id));
        Assert.Contains("chưa đánh giá", ex.Message);
    }
}
