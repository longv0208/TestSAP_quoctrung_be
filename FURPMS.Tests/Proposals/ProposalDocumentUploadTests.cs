using System.Text;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.Extensions.Configuration;

namespace FURPMS.Tests.Proposals;

// Giới hạn upload tài liệu (QĐ 543 Điều 6.4 — hồ sơ là văn bản, không phải file nặng).
public class ProposalDocumentUploadTests
{
    private static (ProposalDocumentService svc, Guid proposalId) MakeService(
        FURPMS.Infrastructure.Data.FURPMSDbContext db, string tempRoot, int? maxFileSizeMb = null)
    {
        // Giới hạn upload nay do Admin đặt trong system_settings — seed như app thật.
        db.SystemSettings.Add(new FURPMS.Domain.Entities.MasterData.SystemSetting
        {
            Key = SystemSettingKeys.UploadMaxFileSizeMb,
            Value = (maxFileSizeMb ?? SystemSettingKeys.RecommendedMaxFileSizeMb).ToString(),
            RecommendedValue = SystemSettingKeys.RecommendedMaxFileSizeMb.ToString()
        });
        db.SystemSettings.Add(new FURPMS.Domain.Entities.MasterData.SystemSetting
        {
            Key = SystemSettingKeys.UploadAllowedExtensions,
            Value = SystemSettingKeys.DefaultAllowedExtensions,
            RecommendedValue = SystemSettingKeys.DefaultAllowedExtensions
        });

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = 1,
            PiUserId = Guid.NewGuid(),
            HostingUnitId = 1,
            ResearchTypeId = 1,
            TitleVi = "Test",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = "Test",
            AbstractVi = "A",
            ResearchObjectives = "O",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = "DRAFT"
        };
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.SaveChanges();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DocumentStorage:RootPath"] = tempRoot })
            .Build();

        var settings = new SystemSettingService(new MasterDataRepository(db));
        return (new ProposalDocumentService(
            new DocumentRepository(db), new ProposalRepository(db), settings, config), proposal.Id);
    }

    [Fact]
    public async Task Upload_FileQuaLon_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var root = Path.Combine(Path.GetTempPath(), $"furpms-{Guid.NewGuid():N}");
        var (svc, proposalId) = MakeService(db, root);

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("x"));
        var tooBig = 11L * 1024 * 1024; // 11 MB > mặc định 10 MB

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.UploadAsync(proposalId, content, "de-cuong.pdf", "application/pdf", tooBig, "Thuyết minh", Guid.NewGuid()));
        Assert.Contains("quá lớn", ex.Message);
    }

    [Fact]
    public async Task Upload_DuoiFileKhongHopLe_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var root = Path.Combine(Path.GetTempPath(), $"furpms-{Guid.NewGuid():N}");
        var (svc, proposalId) = MakeService(db, root);

        using var content = new MemoryStream(Encoding.UTF8.GetBytes("MZ"));
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.UploadAsync(proposalId, content, "virus.exe", "application/octet-stream", 1024, null, Guid.NewGuid()));
        Assert.Contains("không được phép", ex.Message);
    }

    [Fact]
    public async Task Upload_AdminNoiGioiHan_FileLonHonMacDinhVanQua()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var root = Path.Combine(Path.GetTempPath(), $"furpms-{Guid.NewGuid():N}");
        var (svc, proposalId) = MakeService(db, root, maxFileSizeMb: 25); // Admin nâng 10 → 25 MB

        var bytes = Encoding.UTF8.GetBytes("bao cao tong ket");
        using var content = new MemoryStream(bytes);
        var dto = await svc.UploadAsync(
            proposalId, content, "bao-cao.pdf", "application/pdf",
            15L * 1024 * 1024, "Báo cáo", Guid.NewGuid()); // 15 MB: quá mặc định 10, lọt mức 25

        Assert.Equal("bao-cao.pdf", dto.FileName);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task Upload_FileHopLe_LuuDuoc()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var root = Path.Combine(Path.GetTempPath(), $"furpms-{Guid.NewGuid():N}");
        var (svc, proposalId) = MakeService(db, root);

        var bytes = Encoding.UTF8.GetBytes("noi dung ly lich khoa hoc");
        using var content = new MemoryStream(bytes);
        var dto = await svc.UploadAsync(
            proposalId, content, "ly-lich-khoa-hoc.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            bytes.Length, "Lý lịch khoa học", Guid.NewGuid());

        Assert.Equal("ly-lich-khoa-hoc.docx", dto.FileName);
        Assert.Equal(bytes.Length, dto.FileSizeBytes);
        Directory.Delete(root, recursive: true); // dọn file test
    }
}
