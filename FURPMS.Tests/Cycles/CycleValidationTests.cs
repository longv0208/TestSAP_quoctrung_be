using FURPMS.Application.DTOs.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.Cycles;

/// <summary>
/// Kiểm tra dữ liệu khi tạo/sửa đợt.
///
/// <para>
/// Trước 14/08 <b>máy chủ không kiểm gì cả</b> — chỉ form ở giao diện chặn. Gọi thẳng API tạo
/// được đợt có hạn nộp <i>trước</i> ngày mở, tạo hai đợt trùng tên trong cùng năm, đặt năm 1800,
/// để tên rỗng — tất cả đều trả 200. "Giao diện đã chặn" không phải là bảo đảm: giao diện có thể
/// hỏng, có thể bị bỏ qua, và đây đúng là loại chỗ hội đồng hỏi tới.
/// </para>
///
/// <para>
/// Ngược lại, phải cẩn thận đừng kiểm QUÁ tay. Bản 14/08 cấm mỗi năm quá một đợt cho mỗi loại
/// đề tài và ghi chú là theo "QĐ543 Điều 6" — đọc lại thì Điều 6 chỉ nói Phòng QLKH nhận hồ sơ
/// vào quý I / quý II hằng năm, không hề giới hạn số đợt. Ràng buộc đó tự bịa và chặn cả ca
/// hợp lệ (đợt bổ sung), nên đã gỡ ngày 17/08.
/// </para>
/// </summary>
public class CycleValidationTests
{
    private static (CycleService svc, FURPMSDbContext db) Make(string dbName)
    {
        var db = TestDbContextFactory.Create(dbName);
        if (!db.ResearchTypes.Any())
        {
            db.ResearchTypes.Add(new ResearchType { Id = 1, Code = "APPLIED", Name = "Nghiên cứu ứng dụng" });
            db.ResearchTypes.Add(new ResearchType { Id = 2, Code = "BASIC", Name = "Nghiên cứu cơ bản" });
            db.SaveChanges();
        }

        var svc = new CycleService(
            new CycleRepository(db), new MasterDataRepository(db), new ProposalRepository(db),
            new ReviewRepository(db), TestNotifier.Create(db));
        return (svc, db);
    }

    private static CreateCycleRequest Request(
        string name = "Đợt kiểm thử", int year = 0, int researchTypeId = 1,
        string? open = null, string? deadline = null)
    {
        var y = year == 0 ? DateTime.UtcNow.Year : year;
        return new CreateCycleRequest
        {
            Name = name,
            AcademicYear = y.ToString(),
            ResearchTypeId = researchTypeId,
            SubmissionStartDate = open ?? $"{y}-01-01",
            SubmissionDeadline = deadline ?? $"{y}-03-01"
        };
    }

    [Fact]
    public async Task Tao_dot_hop_le_thi_chay_binh_thuong()
    {
        var (svc, _) = Make(nameof(Tao_dot_hop_le_thi_chay_binh_thuong));

        var dto = await svc.CreateCycleAsync(Request(), Guid.NewGuid());

        Assert.False(string.IsNullOrWhiteSpace(dto.Id));
    }

    [Fact]
    public async Task Han_nop_truoc_ngay_mo_thi_bi_chan()
    {
        // Đợt kiểu này không ai nộp đề cương vào được, mà cũng chẳng màn nào báo vì sao.
        var (svc, _) = Make(nameof(Han_nop_truoc_ngay_mo_thi_bi_chan));
        var y = DateTime.UtcNow.Year;

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateCycleAsync(Request(open: $"{y}-05-01", deadline: $"{y}-02-01"), Guid.NewGuid()));

        Assert.Contains("SAU ngày mở nhận", ex.Message);
    }

    [Fact]
    public async Task Han_nop_trung_ngay_mo_cung_bi_chan()
    {
        var (svc, _) = Make(nameof(Han_nop_trung_ngay_mo_cung_bi_chan));
        var y = DateTime.UtcNow.Year;

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateCycleAsync(Request(open: $"{y}-05-01", deadline: $"{y}-05-01"), Guid.NewGuid()));
    }

    [Fact]
    public async Task Trung_ten_trong_cung_nam_thi_bi_chan()
    {
        // Hai đợt cùng hiện "Đợt kiểm thử · 2026" thì Staff chọn nhầm, PI nộp nhầm.
        var (svc, _) = Make(nameof(Trung_ten_trong_cung_nam_thi_bi_chan));
        await svc.CreateCycleAsync(Request(), Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateCycleAsync(Request(), Guid.NewGuid()));

        // Thông báo phải nói bằng lời NGƯỜI DÙNG hiểu, không phải số hiệu quy tắc nội bộ:
        // đọc "(rule #7)" thì người dùng chỉ biết mình sai chứ không biết luật đó là gì.
        Assert.DoesNotContain("rule #", ex.Message);
        Assert.Contains("Đợt kiểm thử", ex.Message);   // tên nào đang trùng
        Assert.Contains("Đặt tên khác", ex.Message);   // làm gì để thoát
    }

    [Fact]
    public async Task Cung_nam_cung_loai_nhung_khac_ten_thi_VAN_tao_duoc()
    {
        // KHÔNG có điều khoản nào của QĐ543 giới hạn mỗi năm một đợt cho mỗi loại. Điều 6 chỉ
        // mô tả nhịp nhận hồ sơ thường lệ (ứng dụng quý I, cơ bản quý II), không phải điều cấm.
        // Trường hoàn toàn có thể mở đợt BỔ SUNG khi đợt đầu không tuyển đủ — chặn cứng là tự
        // bịa luật. Test này khoá lại chuyện đó để không ai siết nhầm lần nữa.
        var (svc, _) = Make(nameof(Cung_nam_cung_loai_nhung_khac_ten_thi_VAN_tao_duoc));
        await svc.CreateCycleAsync(Request(name: "Đợt 1"), Guid.NewGuid());

        var second = await svc.CreateCycleAsync(Request(name: "Đợt bổ sung"), Guid.NewGuid());

        Assert.False(string.IsNullOrWhiteSpace(second.Id));
    }

    [Fact]
    public async Task Cung_nam_khac_loai_de_tai_thi_VAN_tao_duoc()
    {
        // Rule #7 nói "mở cả 2 loại = tạo 2 cycle độc lập" — nên đây phải chạy được.
        var (svc, _) = Make(nameof(Cung_nam_khac_loai_de_tai_thi_VAN_tao_duoc));
        await svc.CreateCycleAsync(Request(name: "Đợt ứng dụng", researchTypeId: 1), Guid.NewGuid());

        var second = await svc.CreateCycleAsync(Request(name: "Đợt cơ bản", researchTypeId: 2), Guid.NewGuid());

        Assert.False(string.IsNullOrWhiteSpace(second.Id));
    }

    [Fact]
    public async Task Nam_qua_xa_thi_bi_chan()
    {
        var (svc, _) = Make(nameof(Nam_qua_xa_thi_bi_chan));

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateCycleAsync(Request(year: 1800), Guid.NewGuid()));

        Assert.Contains("1800", ex.Message);
    }

    [Fact]
    public async Task Ten_rong_thi_bi_chan()
    {
        var (svc, _) = Make(nameof(Ten_rong_thi_bi_chan));

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateCycleAsync(Request(name: "   "), Guid.NewGuid()));
    }

    [Fact]
    public async Task Sua_dot_thanh_han_nop_truoc_ngay_mo_cung_bi_chan()
    {
        // Sửa từng phần nên hạn nộp MỚI có thể rơi trước ngày mở CŨ — kiểm riêng lẻ từng trường
        // sẽ không thấy, phải kiểm trạng thái cuối cùng.
        var (svc, _) = Make(nameof(Sua_dot_thanh_han_nop_truoc_ngay_mo_cung_bi_chan));
        var y = DateTime.UtcNow.Year;
        var created = await svc.CreateCycleAsync(Request(open: $"{y}-05-01", deadline: $"{y}-08-01"), Guid.NewGuid());

        var patch = new CreateCycleRequest { SubmissionDeadline = $"{y}-02-01" };

        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateCycleAsync(int.Parse(created.Id), patch));
    }

    [Fact]
    public async Task Sua_chinh_dot_do_khong_bi_bao_trung_voi_chinh_no()
    {
        var (svc, _) = Make(nameof(Sua_chinh_dot_do_khong_bi_bao_trung_voi_chinh_no));
        var created = await svc.CreateCycleAsync(Request(), Guid.NewGuid());

        var renamed = await svc.UpdateCycleAsync(int.Parse(created.Id), new CreateCycleRequest { Name = "Đổi tên thôi" });

        Assert.Equal(created.Id, renamed.Id);
    }
}
