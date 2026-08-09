using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Domain.Entities.AI;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;

namespace FURPMS.Tests.Progress;

/// <summary>
/// Thầy 29/07: *"Staff phải xem được bản báo cáo mới đánh giá"*. Luật này từng CHỈ khoá ở giao diện
/// (nút mờ đi) — gọi thẳng API là đánh giá được một báo cáo trắng trơn, không có gì để đọc.
/// Rà F1 ngày 09/08 phát hiện, nay chặn ở phía máy chủ.
/// </summary>
public class ProgressReportEvaluationTests
{
    private static ProgressReportService MakeService(FURPMSDbContext db) =>
        new(new ContractRepository(db), new ProposalRepository(db), new FakeClock(), new DocumentRepository(db));

    private static async Task<(FURPMSDbContext db, ProgressReport report)> SeedSubmittedReportAsync(
        string? reportFileUrl, bool withAttachment)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = new User
        {
            Id = Guid.NewGuid(),
            Email = $"pi-{Guid.NewGuid():N}"[..18] + "@t.com",
            FullName = "PI",
            Status = UserStatus.Active
        };
        var rt = new ResearchType { Code = $"RT{Guid.NewGuid():N}"[..8], Name = "Loai", IsActive = true };
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = 1,
            PiUserId = pi.Id,
            HostingUnitId = 1,
            ResearchTypeId = rt.Id,
            TitleVi = "De tai",
            Status = ProjectStatus.InProgress,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        db.Projects.Add(project);
        db.Proposals.Add(new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = "De tai",
            AbstractVi = "A",
            ResearchObjectives = "O",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = ProposalStatus.Approved
        });

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ContractNumber = $"CTR-{Guid.NewGuid():N}"[..12],
            TotalAmount = 1_000_000m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            OriginalEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = ContractStatus.Active,
            CreatedBy = pi.Id
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var report = new ProgressReport
        {
            ContractId = contract.Id,
            ReportRound = 1,
            ReportingPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            ReportingPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow),
            CompletedContent = "Da lam xong phan 1",
            OverallCompletionPct = 50m,
            Status = ProgressReportStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            ReportFileUrl = reportFileUrl
        };
        db.ProgressReports.Add(report);
        await db.SaveChangesAsync();

        if (withAttachment)
        {
            db.Documents.Add(new Document
            {
                EntityType = "ProgressReport",
                EntityId = report.Id.ToString(),
                DocumentCategory = "PROGRESS_REPORT",
                OriginalFileName = "bao-cao-ky-1.pdf",
                FileSizeBytes = 1024,
                MimeType = "application/pdf",
                StorageContainer = "test",
                StorageBlobName = "test/bao-cao.pdf",
                StorageUrl = "/download",
                UploadedBy = pi.Id
            });
            await db.SaveChangesAsync();
        }

        return (db, report);
    }

    [Fact]
    public async Task Evaluate_NoFileNoLink_Throws()
    {
        var (db, report) = await SeedSubmittedReportAsync(reportFileUrl: null, withAttachment: false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MakeService(db).EvaluateAsync(report.Id,
                new EvaluateProgressReportRequest { EvaluationResult = "PASS" }, Guid.NewGuid()));

        Assert.Contains("chưa có file đính kèm lẫn link", ex.Message);
        // Không được đánh giá nửa vời: báo cáo vẫn ở trạng thái đã nộp.
        Assert.Equal(ProgressReportStatus.Submitted,
            (await db.ProgressReports.FindAsync(report.Id))!.Status);
    }

    /// <summary>File quá lớn thì PI dán link — vẫn phải đánh giá được, không chặn oan.</summary>
    [Fact]
    public async Task Evaluate_WithLinkOnly_Succeeds()
    {
        var (db, report) = await SeedSubmittedReportAsync(
            reportFileUrl: "https://drive.google.com/file/d/abc/view", withAttachment: false);

        var result = await MakeService(db).EvaluateAsync(report.Id,
            new EvaluateProgressReportRequest { EvaluationResult = "PASS" }, Guid.NewGuid());

        Assert.Equal(ProgressReportStatus.Evaluated, result.Status);
    }

    [Fact]
    public async Task Evaluate_WithAttachmentOnly_Succeeds()
    {
        var (db, report) = await SeedSubmittedReportAsync(reportFileUrl: null, withAttachment: true);

        var result = await MakeService(db).EvaluateAsync(report.Id,
            new EvaluateProgressReportRequest { EvaluationResult = "PASS" }, Guid.NewGuid());

        Assert.Equal(ProgressReportStatus.Evaluated, result.Status);
    }

    /// <summary>Kết quả đánh giá chỉ có 3 giá trị theo BM06 — "ACHIEVED" không nằm trong đó.</summary>
    [Fact]
    public async Task Evaluate_InvalidResult_Throws()
    {
        var (db, report) = await SeedSubmittedReportAsync(
            reportFileUrl: "https://example.com/bc.pdf", withAttachment: false);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            MakeService(db).EvaluateAsync(report.Id,
                new EvaluateProgressReportRequest { EvaluationResult = "ACHIEVED" }, Guid.NewGuid()));

        Assert.Contains("PASS", ex.Message);
    }
}
