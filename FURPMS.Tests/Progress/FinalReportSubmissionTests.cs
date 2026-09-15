using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Progress;

public class FinalReportSubmissionTests
{
    private static readonly DateOnly End = new(2026, 12, 31);

    private static async Task<(FURPMSDbContext db, Guid piId, Contract contract, Project project)> SeedAsync()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var piId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(), PiUserId = piId, CycleTrackId = 1, OrderId = 1, HostingUnitId = 1,
            ResearchTypeId = 1, TitleVi = "Đề tài", Status = ProjectStatus.Approved,
            PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = End
        };
        var contract = new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "CTR-FINAL-NEW",
            TotalAmount = 1_000_000m, StartDate = new DateOnly(2026, 1, 1), EndDate = End,
            OriginalEndDate = End, Status = ContractStatus.Active, CreatedBy = piId
        };
        db.Projects.Add(project);
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        return (db, piId, contract, project);
    }

    private static FinalReportService Service(FURPMSDbContext db) =>
        TestServices.FinalReports(db, new FakeClock { UtcNow = new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc) });

    private static SubmitFinalReportRequest Request() => new()
    {
        ReportFileUrl = "https://example.com/final.pdf", Language = "VI"
    };

    [Fact]
    public async Task Submit_FirstReport_SetsProjectAcceptanceAndLeadDeadline()
    {
        var (db, piId, contract, project) = await SeedAsync();

        var result = await Service(db).SubmitAsync(contract.Id, Request(), piId);

        Assert.Equal(FinalReportStatus.Submitted, result.Status);
        Assert.Equal(ProjectStatus.Acceptance, (await db.Projects.FindAsync(project.Id))!.Status);
        Assert.Equal("2026-12-01", result.Deadline);
        Assert.Equal("VI", result.Language);
    }

    [Fact]
    public async Task Submit_ByNonPi_IsForbiddenAndDoesNotCreateReport()
    {
        var (db, _, contract, _) = await SeedAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => Service(db).SubmitAsync(contract.Id, Request(), Guid.NewGuid()));

        Assert.Empty(await db.FinalReports.ToListAsync());
    }

    [Fact]
    public async Task Submit_WithoutReportDocument_IsRejected()
    {
        var (db, piId, contract, _) = await SeedAsync();
        var request = Request();
        request.ReportFileUrl = "  ";

        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).SubmitAsync(contract.Id, request, piId));
        Assert.Empty(await db.FinalReports.ToListAsync());
    }

    [Fact]
    public async Task Submit_UnknownContract_IsNotFound()
    {
        var (db, piId, _, _) = await SeedAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(db).SubmitAsync(Guid.NewGuid(), Request(), piId));
    }
}
