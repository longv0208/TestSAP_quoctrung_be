using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Users;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Users;

/// <summary>
/// Lý lịch khoa học — công trình &amp; đề tài (QĐ543 <b>Biểu mẫu 02</b>).
///
/// <para>
/// BM02 đòi <b>cả hai</b>: số lượng (mục 14.1–14.5, 15, 19.1/19.3) <i>và</i> danh sách chi tiết
/// (14.6, 17, 19.4). Trước đây hệ thống chỉ có ô đếm nhập tay, phần liệt kê bỏ trắng — hồ sơ
/// thiếu so với biểu mẫu và hội đồng không tra được nguồn. Nay danh sách là nguồn sự thật, số do
/// máy chủ cộng lại, nên hai phần <b>không thể lệch nhau</b>.
/// </para>
/// </summary>
public class AcademicWorksTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private static AcademicWorkService MakeService(string dbName) =>
        new(new MasterDataRepository(TestDbContextFactory.Create(dbName)));

    private static AcademicWorkRequest Publication(string title = "Bài báo kiểm thử") => new()
    {
        WorkType = WorkTypes.Publication,
        Category = WorkCategories.IsiScopus,
        Title = title,
        Venue = "IEEE Access",
        Authors = "Nguyễn Văn A",
        Role = WorkRoles.MainAuthor,
        Year = 2024,
        Volume = "12",
        Pages = "145-158",
        Identifier = "10.1109/ACCESS.2024.1"
    };

    [Fact]
    public async Task Them_cong_bo_thi_o_dem_tuong_ung_tu_tang()
    {
        var name = nameof(Them_cong_bo_thi_o_dem_tuong_ung_tu_tang);
        var service = MakeService(name);

        await service.CreateAsync(Owner, Owner, Publication());
        await service.CreateAsync(Owner, Owner, Publication("Bài báo thứ hai"));

        var db = TestDbContextFactory.Create(name);
        var profile = await db.AcademicProfiles.FirstAsync(p => p.UserId == Owner);

        // Đúng ô 14.1 tăng, các ô khác không đụng tới.
        Assert.Equal(2, profile.IsiScopusCount);
        Assert.Equal(0, profile.IntlJournalCount);
        Assert.Equal(0, profile.PatentsCount);
    }

    [Fact]
    public async Task Xoa_cong_bo_thi_o_dem_tu_giam()
    {
        var name = nameof(Xoa_cong_bo_thi_o_dem_tu_giam);
        var service = MakeService(name);

        var work = await service.CreateAsync(Owner, Owner, Publication());
        await service.DeleteAsync(Owner, Owner, work.Id);

        var db = TestDbContextFactory.Create(name);
        var profile = await db.AcademicProfiles.FirstAsync(p => p.UserId == Owner);
        Assert.Equal(0, profile.IsiScopusCount);
    }

    [Fact]
    public async Task Doi_phan_loai_thi_o_dem_chuyen_theo()
    {
        // Đây là chỗ khai tay hai nơi chắc chắn lệch: sửa phân loại xong quên sửa số.
        var name = nameof(Doi_phan_loai_thi_o_dem_chuyen_theo);
        var service = MakeService(name);

        var work = await service.CreateAsync(Owner, Owner, Publication());

        var moved = Publication();
        moved.Category = WorkCategories.JournalDomestic;   // 14.1 → 14.3
        await service.UpdateAsync(Owner, Owner, work.Id, moved);

        var db = TestDbContextFactory.Create(name);
        var profile = await db.AcademicProfiles.FirstAsync(p => p.UserId == Owner);
        Assert.Equal(0, profile.IsiScopusCount);
        Assert.Equal(1, profile.DomesticJournalCount);
    }

    [Fact]
    public async Task Phan_loai_khong_thuoc_muc_thi_bi_chan_va_noi_ro_gia_tri_hop_le()
    {
        var service = MakeService(nameof(Phan_loai_khong_thuoc_muc_thi_bi_chan_va_noi_ro_gia_tri_hop_le));

        // Mục 17 (đề tài) không dùng phân loại của mục 14 (công bố).
        var bad = Publication();
        bad.WorkType = WorkTypes.Project;
        bad.Category = WorkCategories.IsiScopus;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Owner, Owner, bad));

        // Báo lỗi phải kèm giá trị hợp lệ, không thì người dùng chỉ biết "sai" mà không biết sửa sao.
        Assert.Contains("PROJECT_LEAD", ex.Message);
        Assert.Contains("PROJECT_MEMBER", ex.Message);
    }

    [Fact]
    public async Task Nam_cong_bo_o_tuong_lai_bi_chan()
    {
        var service = MakeService(nameof(Nam_cong_bo_o_tuong_lai_bi_chan));

        var bad = Publication();
        bad.Year = DateTime.UtcNow.Year + 5;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Owner, Owner, bad));
        Assert.Contains(bad.Year.ToString()!, ex.Message);
    }

    [Fact]
    public async Task De_tai_bat_dau_sau_khi_ket_thuc_bi_chan()
    {
        var service = MakeService(nameof(De_tai_bat_dau_sau_khi_ket_thuc_bi_chan));

        var bad = new AcademicWorkRequest
        {
            WorkType = WorkTypes.Project,
            Category = WorkCategories.ProjectLead,
            Title = "Đề tài lộn ngược thời gian",
            StartYear = 2024,
            Year = 2022
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Owner, Owner, bad));
    }

    [Fact]
    public async Task Admin_khong_khai_ho_duoc_ly_lich_nguoi_khac()
    {
        // Lý lịch khoa học là lời khai có trách nhiệm của người đứng tên — để người khác khai hộ
        // là làm hỏng chính giá trị pháp lý của nó, nên kể cả Admin cũng bị chặn.
        var service = MakeService(nameof(Admin_khong_khai_ho_duoc_ly_lich_nguoi_khac));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(Owner, Stranger, Publication()));
    }

    [Fact]
    public async Task Admin_van_XEM_duoc_ly_lich_nguoi_khac_de_tham_dinh()
    {
        var name = nameof(Admin_van_XEM_duoc_ly_lich_nguoi_khac_de_tham_dinh);
        var service = MakeService(name);
        await service.CreateAsync(Owner, Owner, Publication());

        var works = await service.ListAsync(Owner, Stranger, requesterIsAdminOrStaff: true);

        Assert.Single(works);
        Assert.Equal("IEEE Access", works[0].Venue);
    }

    [Fact]
    public async Task Nguoi_la_khong_xem_duoc_ly_lich_nguoi_khac()
    {
        var service = MakeService(nameof(Nguoi_la_khong_xem_duoc_ly_lich_nguoi_khac));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ListAsync(Owner, Stranger, requesterIsAdminOrStaff: false));
    }

    [Fact]
    public async Task Luu_du_volume_va_trang_so_theo_muc_14_6()
    {
        // BM02 mục 14.6 đòi đích danh "tên tạp chí, volume, trang số" — thiếu là hồ sơ không đủ.
        var name = nameof(Luu_du_volume_va_trang_so_theo_muc_14_6);
        var service = MakeService(name);

        await service.CreateAsync(Owner, Owner, Publication());

        var db = TestDbContextFactory.Create(name);
        var work = await db.AcademicWorks.FirstAsync(w => w.UserId == Owner);

        Assert.Equal("IEEE Access", work.Venue);
        Assert.Equal("12", work.Volume);
        Assert.Equal("145-158", work.Pages);
        Assert.Equal("10.1109/ACCESS.2024.1", work.Identifier);
    }

    [Fact]
    public async Task Tinh_trang_nhiem_vu_chi_nhan_3_gia_tri_bieu_mau_liet_ke()
    {
        var service = MakeService(nameof(Tinh_trang_nhiem_vu_chi_nhan_3_gia_tri_bieu_mau_liet_ke));

        var bad = new AcademicWorkRequest
        {
            WorkType = WorkTypes.Project,
            Category = WorkCategories.ProjectLead,
            Title = "Đề tài có tình trạng lạ",
            Status = "DANG_LAM_DO"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Owner, Owner, bad));
    }

    [Fact]
    public async Task Khai_cong_trinh_khi_chua_co_ho_so_thi_tu_tao_vo_ho_so()
    {
        // Người mới tạo tài khoản chưa mở trang Hồ sơ bao giờ ⇒ chưa có bản ghi academic_profile.
        // Không tạo vỏ thì chỗ chứa số suy ra không tồn tại và thao tác đầu tiên sẽ nổ.
        var name = nameof(Khai_cong_trinh_khi_chua_co_ho_so_thi_tu_tao_vo_ho_so);
        var service = MakeService(name);

        await service.CreateAsync(Owner, Owner, Publication());

        var db = TestDbContextFactory.Create(name);
        Assert.True(await db.AcademicProfiles.AnyAsync(p => p.UserId == Owner));
    }
}
