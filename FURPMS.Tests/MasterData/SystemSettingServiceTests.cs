using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.MasterData;

// Cấu hình vận hành: Admin chỉnh dung lượng upload, có mức khuyến cáo + chặn trên/dưới.
public class SystemSettingServiceTests
{
    private static SystemSettingService MakeService(FURPMS.Infrastructure.Data.FURPMSDbContext db, string value = "10")
    {
        db.SystemSettings.Add(new SystemSetting
        {
            Key = SystemSettingKeys.UploadMaxFileSizeMb,
            Value = value,
            RecommendedValue = SystemSettingKeys.RecommendedMaxFileSizeMb.ToString()
        });
        db.SaveChanges();
        return new SystemSettingService(new MasterDataRepository(db));
    }

    [Fact]
    public async Task UploadPolicy_MacDinh_TraMucKhuyenCao()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = MakeService(db);

        var policy = await svc.GetUploadPolicyAsync();

        Assert.Equal(SystemSettingKeys.RecommendedMaxFileSizeMb, policy.MaxFileSizeMb);
        Assert.Equal(SystemSettingKeys.RecommendedMaxFileSizeMb, policy.RecommendedMaxFileSizeMb);
        Assert.Contains(".pdf", policy.AllowedExtensions); // không có bản ghi extensions → rơi về mặc định
    }

    [Fact]
    public async Task Update_TrongKhoangChoPhep_LuuDuoc()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = MakeService(db);

        var updated = await svc.UpdateAsync(SystemSettingKeys.UploadMaxFileSizeMb, "25", Guid.NewGuid());

        Assert.Equal("25", updated.Value);
        Assert.Equal(25, (await svc.GetUploadPolicyAsync()).MaxFileSizeMb);
    }

    [Fact]
    public async Task Update_VuotChanTren_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = MakeService(db);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.UpdateAsync(SystemSettingKeys.UploadMaxFileSizeMb, "10240", Guid.NewGuid()));
        Assert.Contains("khoảng", ex.Message);
    }

    [Fact]
    public async Task Update_KhongPhaiSo_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = MakeService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.UpdateAsync(SystemSettingKeys.UploadMaxFileSizeMb, "to đùng", Guid.NewGuid()));
    }

    [Fact]
    public async Task UploadPolicy_GiaTriHong_RoiVeMucKhuyenCao()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = MakeService(db, value: "abc"); // dữ liệu cũ/hỏng trong DB

        var policy = await svc.GetUploadPolicyAsync();

        Assert.Equal(SystemSettingKeys.RecommendedMaxFileSizeMb, policy.MaxFileSizeMb);
    }

    // ── Các cấu hình vận hành khác (thêm 21/07) ───────────────────────────────

    private static SystemSettingService WithKey(FURPMS.Infrastructure.Data.FURPMSDbContext db, string key, string value)
    {
        db.SystemSettings.Add(new SystemSetting { Key = key, Value = value, RecommendedValue = value });
        db.SaveChanges();
        return new SystemSettingService(new MasterDataRepository(db));
    }

    [Fact]
    public async Task WholeTranches_DuoiMucToiThieu_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = WithKey(db, SystemSettingKeys.DisbursementWholeTranches, "3");

        // QĐ 543: cấp trọn gói phải tối thiểu 3 đợt (đầu/giữa/cuối) — rule #6.
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.UpdateAsync(SystemSettingKeys.DisbursementWholeTranches, "2", Guid.NewGuid()));
    }

    [Fact]
    public async Task ReminderDays_SapXepGiamDan_VaBoTrung()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = WithKey(db, SystemSettingKeys.DeadlineReminderDays, "30,14,7");

        var updated = await svc.UpdateAsync(SystemSettingKeys.DeadlineReminderDays, "7, 30, 14, 7", Guid.NewGuid());

        Assert.Equal("30,14,7", updated.Value);
    }

    [Fact]
    public async Task ReminderDays_GiaTriRac_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = WithKey(db, SystemSettingKeys.DeadlineReminderDays, "30,14,7");

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.UpdateAsync(SystemSettingKeys.DeadlineReminderDays, "ba muoi", Guid.NewGuid()));
    }

    [Fact]
    public async Task GetIntList_ThieuBanGhi_RoiVeMacDinh()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = new SystemSettingService(new MasterDataRepository(db));

        var days = await svc.GetIntListAsync(SystemSettingKeys.DeadlineReminderDays, new[] { 30, 14, 7 });

        Assert.Equal(new[] { 30, 14, 7 }, days);
    }

    [Fact]
    public async Task EmailEnabled_DocDuocKieuBool()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = WithKey(db, SystemSettingKeys.EmailEnabled, "false");

        Assert.False(await svc.GetBoolAsync(SystemSettingKeys.EmailEnabled, true));
    }
}
