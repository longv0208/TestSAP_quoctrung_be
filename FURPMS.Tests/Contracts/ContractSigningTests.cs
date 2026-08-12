using FURPMS.Application.DTOs.Contract;
using FURPMS.Domain.Entities.AI;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;

namespace FURPMS.Tests.Contracts;

/// <summary>
/// Ghi nhận ký hợp đồng — QĐ543 <b>BM05 Điều 7.2</b>: hợp đồng ký điện tử trên <b>phần mềm ngoài</b>
/// (Econtract), hệ thống này chỉ sinh biểu mẫu và <b>giữ bản đã ký làm bằng chứng</b> (rule #21).
/// Và <b>BM05 Điều 6.1</b>: sau khi ký, mọi thay đổi phải lập thành <b>phụ lục</b> — không sửa đè.
/// </summary>
public class ContractSigningTests
{
    private static async Task<(ContractService svc, FURPMSDbContext db, Contract contract)> SetupAsync()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = new User { Id = Guid.NewGuid(), Email = $"pi-{Guid.NewGuid()}@t.com", FullName = "Chủ nhiệm", Status = "ACTIVE" };
        var type = new ResearchType { Code = "BASIC", Name = "Nghiên cứu cơ bản", MaxBudgetCap = 100_000_000m, IsActive = true };
        db.Users.Add(pi);
        db.ResearchTypes.Add(type);
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(), CycleTrackId = 1, OrderId = 1, PiUserId = pi.Id, HostingUnitId = 1,
            ResearchTypeId = type.Id, TitleVi = "Đề tài kiểm thử ký hợp đồng", Status = "APPROVED",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        db.Projects.Add(project);
        db.Proposals.Add(new Proposal
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
            TitleVi = project.TitleVi, AbstractVi = "…", ResearchObjectives = "…", DurationMonths = 12,
            PlannedStartDate = project.PlannedStartDate, PlannedEndDate = project.PlannedEndDate,
            Status = "APPROVED"
        });
        await db.SaveChangesAsync();

        var contract = new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "HĐ-TEST-01",
            Status = "PENDING_SIGNATURE", TotalAmount = 90_000_000m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            MaxExtensionMonths = 6, CreatedBy = pi.Id
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var svc = new ContractService(
            new ContractRepository(db), new ProposalRepository(db), new FakeClock(),
            new SystemSettingService(new MasterDataRepository(db)), TestNotifier.Create(db),
            new DocumentRepository(db));

        return (svc, db, contract);
    }

    private static void AddSignedCopy(FURPMSDbContext db, Guid contractId)
    {
        db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            EntityType = "Contract",
            EntityId = contractId.ToString(),
            OriginalFileName = "HopDong-daky.pdf",
            DocumentCategory = "SIGNED_CONTRACT",
            StorageContainer = "contracts",
            StorageBlobName = "x.pdf",
            StorageUrl = "/files/contracts/x.pdf",
            MimeType = "application/pdf",
            FileSizeBytes = 1024,
            UploadedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    // ── 1: chưa có bản ký ⇒ KHÔNG ghi nhận được, và lỗi phải chỉ đường ───────
    [Fact]
    public async Task Sign_WithoutSignedCopy_Throws_AndTellsWhatToDo()
    {
        var (svc, _, contract) = await SetupAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SignAsync(contract.Id, Guid.NewGuid()));

        Assert.Contains("Chưa có bản hợp đồng đã ký", ex.Message);
        // Người dùng phải đọc được BA bước phải làm, không chỉ "bị từ chối".
        Assert.Contains("Xuất hợp đồng", ex.Message);
        Assert.Contains("7.2", ex.Message);
    }

    // ── 2: có bản ký ⇒ ghi nhận được, lấy đúng NGÀY KÝ TRÊN GIẤY ────────────
    [Fact]
    public async Task Sign_WithSignedCopy_UsesPaperSignDate()
    {
        var (svc, db, contract) = await SetupAsync();
        AddSignedCopy(db, contract.Id);

        // Ký trên giấy hôm kia, hôm nay mới nhập vào hệ thống.
        var paperDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var result = await svc.SignAsync(contract.Id, Guid.NewGuid(), paperDate);

        Assert.Equal("ACTIVE", result.Status);
        Assert.NotNull(result.SignedAt);
        Assert.Equal(paperDate, DateOnly.FromDateTime(result.SignedAt!.Value));
    }

    // ── 3: ngày ký ở TƯƠNG LAI ⇒ chặn ───────────────────────────────────────
    [Fact]
    public async Task Sign_FutureDate_Throws()
    {
        var (svc, db, contract) = await SetupAsync();
        AddSignedCopy(db, contract.Id);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SignAsync(contract.Id, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
    }

    // ── 4: đã ký ⇒ KHÔNG sửa đè, phải đi đường phụ lục (BM05 Điều 6.1) ──────
    [Fact]
    public async Task Update_AfterSigned_Throws_AndPointsToAmendment()
    {
        var (svc, db, contract) = await SetupAsync();
        AddSignedCopy(db, contract.Id);
        await svc.SignAsync(contract.Id, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.UpdateAsync(contract.Id, new UpdateContractRequest
            {
                ContractNumber = "HĐ-TEST-01-SUA",
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                MaxExtensionMonths = 6
            }));

        Assert.Contains("PHỤ LỤC", ex.Message);
        Assert.Contains("6.1", ex.Message);
    }

    // ── 5: chưa ký thì vẫn sửa bình thường ──────────────────────────────────
    [Fact]
    public async Task Update_BeforeSigned_Succeeds()
    {
        var (svc, _, contract) = await SetupAsync();

        var result = await svc.UpdateAsync(contract.Id, new UpdateContractRequest
        {
            ContractNumber = "HĐ-TEST-01-SUA",
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            MaxExtensionMonths = 6
        });

        Assert.Equal("HĐ-TEST-01-SUA", result.ContractNumber);
    }
}
