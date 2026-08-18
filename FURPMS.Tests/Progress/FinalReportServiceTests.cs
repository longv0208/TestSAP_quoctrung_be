using FURPMS.Application.DTOs.Progress;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;

namespace FURPMS.Tests.Progress;

public class FinalReportServiceTests
{
    private static async Task<(FinalReportService service, Contract contract, FinalReport report)> SeedAsync(string status)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var piId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            PiUserId = piId,
            CycleTrackId = 1,
            OrderId = 1,
            HostingUnitId = 1,
            ResearchTypeId = 1,
            TitleVi = "Đề tài",
            Status = "ACCEPTANCE",
            PlannedStartDate = new DateOnly(2026, 1, 1),
            PlannedEndDate = new DateOnly(2026, 12, 31)
        };
        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ContractNumber = "CTR-FINAL",
            TotalAmount = 1_000_000m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            OriginalEndDate = new DateOnly(2026, 12, 31),
            Status = "ACTIVE",
            CreatedBy = piId
        };
        var report = new FinalReport
        {
            ProjectId = project.Id,
            Status = status,
            ReportFileUrl = "https://example.com/old.pdf",
            Language = "VI",
            SubmittedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        db.Projects.Add(project);
        db.Contracts.Add(contract);
        db.FinalReports.Add(report);
        await db.SaveChangesAsync();

        var clock = new FakeClock { UtcNow = new DateTime(2026, 8, 19, 4, 0, 0, DateTimeKind.Utc) };
        return (new FinalReportService(new ContractRepository(db), clock), contract, report);
    }

    [Fact]
    public async Task Submit_WhileSubmitted_UpdatesCurrentVersion()
    {
        var (service, contract, report) = await SeedAsync("SUBMITTED");

        var result = await service.SubmitAsync(contract.Id, new SubmitFinalReportRequest
        {
            ReportFileUrl = "https://example.com/new.pdf",
            Language = "EN"
        }, contract.CreatedBy);

        Assert.Equal(report.Id, result.Id);
        Assert.Equal("https://example.com/new.pdf", result.ReportFileUrl);
        Assert.Equal("EN", result.Language);
        Assert.NotNull(result.FinalSubmittedAt);
    }

    [Fact]
    public async Task Submit_AfterAccepted_IsBlocked()
    {
        var (service, contract, _) = await SeedAsync("ACCEPTED");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(contract.Id,
            new SubmitFinalReportRequest { ReportFileUrl = "https://example.com/new.pdf", Language = "VI" },
            contract.CreatedBy));

        Assert.Contains("không sửa được", ex.Message);
    }

    [Fact]
    public async Task Submit_WithFreeTextLanguage_IsBlocked()
    {
        var (service, contract, _) = await SeedAsync("SUBMITTED");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync(contract.Id,
            new SubmitFinalReportRequest { ReportFileUrl = "https://example.com/new.pdf", Language = "abc.com" },
            contract.CreatedBy));

        Assert.Contains("Tiếng Việt hoặc Tiếng Anh", ex.Message);
    }

    [Fact]
    public async Task Submit_WithBareDomain_NormalizesExternalLinks()
    {
        var (service, contract, _) = await SeedAsync("SUBMITTED");

        var result = await service.SubmitAsync(contract.Id, new SubmitFinalReportRequest
        {
            ReportFileUrl = "drive.google.com/report",
            SummaryFileUrl = "/api/final-reports/document/download",
            Language = "VI"
        }, contract.CreatedBy);

        Assert.Equal("https://drive.google.com/report", result.ReportFileUrl);
        Assert.Equal("/api/final-reports/document/download", result.SummaryFileUrl);
    }

    [Fact]
    public async Task Submit_WithRelativeAppRoute_IsBlocked()
    {
        var (service, contract, _) = await SeedAsync("SUBMITTED");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync(contract.Id,
            new SubmitFinalReportRequest { ReportFileUrl = "ab", Language = "VI" },
            contract.CreatedBy));

        Assert.Contains("Đường dẫn báo cáo tổng kết không hợp lệ", ex.Message);
    }

    [Fact]
    public async Task Archive_WithoutSummary_IsBlocked()
    {
        var (service, _, report) = await SeedAsync("ACCEPTED");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ArchiveAsync(report.Id, Guid.NewGuid()));

        Assert.Contains("bản tóm tắt", ex.Message);
    }
}
