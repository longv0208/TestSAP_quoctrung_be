using FURPMS.Application.DTOs.Cycles;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Cycles;

// Rule tuần 10 (thầy Đức): gia hạn deadline đợt = GHI LOG, không ghi đè ngày gốc; deadline hiệu lực
// = bản mới nhất; lần gia hạn sau lấy bản trước làm mốc; lùi ngày → lỗi.
public class CycleDeadlineExtensionTests
{
    private static CycleService MakeSvc(FURPMS.Infrastructure.Data.FURPMSDbContext db) =>
        new(new CycleRepository(db), new MasterDataRepository(db), new ProposalRepository(db));

    private static ResearchCycle MakeCycle() => new()
    {
        CycleYear = 2026,
        ResearchTypeId = 1,
        SubmissionOpenDate = new DateOnly(2026, 1, 1),
        SubmissionDeadline = new DateOnly(2026, 3, 1),
        ReviewDeadline = new DateOnly(2026, 6, 1),
        Status = "OPEN",
        CreatedBy = Guid.NewGuid()
    };

    [Fact]
    public async Task ExtendDeadline_LogsWithoutOverwritingOriginal_AndChainsFromLatest()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var cycle = MakeCycle();
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var svc = MakeSvc(db);
        var ext1 = await svc.ExtendCycleDeadlineAsync(cycle.Id, new ExtendDeadlineRequest { NewDeadline = "2026-04-01", Reason = "Thêm 1 tháng" }, Guid.NewGuid());
        Assert.Equal("2026-03-01", ext1.OldDeadline);   // gốc
        Assert.Equal("2026-04-01", ext1.NewDeadline);

        // Ngày gốc trong ResearchCycle KHÔNG bị ghi đè.
        var fresh = await db.ResearchCycles.FindAsync(cycle.Id);
        Assert.Equal(new DateOnly(2026, 3, 1), fresh!.SubmissionDeadline);

        // Lần 2 lấy bản trước (04-01) làm mốc.
        var ext2 = await svc.ExtendCycleDeadlineAsync(cycle.Id, new ExtendDeadlineRequest { NewDeadline = "2026-05-01" }, Guid.NewGuid());
        Assert.Equal("2026-04-01", ext2.OldDeadline);
        Assert.Equal("2026-05-01", ext2.NewDeadline);

        Assert.Equal(2, await db.DeadlineExtensions.CountAsync());
    }

    [Fact]
    public async Task ExtendDeadline_EarlierThanCurrent_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var cycle = MakeCycle();
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var svc = MakeSvc(db);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.ExtendCycleDeadlineAsync(cycle.Id, new ExtendDeadlineRequest { NewDeadline = "2026-02-01" }, Guid.NewGuid()));
        Assert.Equal(0, await db.DeadlineExtensions.CountAsync());
    }
}
