using System.Globalization;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <summary>
/// Trần kinh phí đề tài — QĐ543 <b>Điều 14</b>.
/// <list type="bullet">
/// <item>14.1.a — nghiên cứu <b>cơ bản</b>: tối đa <b>100.000.000đ</b>/đề tài.</item>
/// <item>14.1.b — nghiên cứu <b>ứng dụng/triển khai</b>: tối đa <b>150.000.000đ</b>/đề tài.</item>
/// <item>14.3 — <i>"Mức kinh phí vượt mức tối đa trên do Hiệu trưởng xem xét, quyết định."</i></item>
/// </list>
/// <para>
/// Vì khoản 3 cho phép vượt trần, hệ thống <b>không</b> khoá cứng con số trong mã nguồn: trần nằm ở
/// master data <c>research_types.max_budget_cap</c>. Đề tài được Hiệu trưởng duyệt vượt trần thì
/// Admin nâng trần ở màn Loại đề tài — có dấu vết ai sửa, thay vì để mọi người tự do vượt.
/// </para>
/// </summary>
public class BudgetPolicyService : IBudgetPolicyService
{
    private readonly IProposalRepository _proposals;
    private readonly ICycleRepository _cycles;
    private readonly IMasterDataRepository _masterData;

    public BudgetPolicyService(
        IProposalRepository proposals,
        ICycleRepository cycles,
        IMasterDataRepository masterData)
    {
        _proposals = proposals;
        _cycles = cycles;
        _masterData = masterData;
    }

    public async Task<BudgetCapInfo> GetCapAsync(Guid proposalId)
    {
        // Chỉ lấy KHOÁ ở đây, không join sang ResearchType/Order. Join qua navigation bắt buộc mà
        // bản ghi đích khuyết là INNER JOIN — cả dòng gốc biến mất và đề cương có thật lại báo 404
        // (đúng cái bẫy đã dính ở ExportAmendmentDocAsync).
        var info = await _proposals.Query().IgnoreQueryFilters()
            .Where(p => p.Id == proposalId)
            .Select(p => new { p.Project.ResearchTypeId, p.Project.OrderId })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Proposal {proposalId} not found.");

        var type = await _masterData.ResearchTypes
            .Where(t => t.Id == info.ResearchTypeId)
            .Select(t => new { t.Name, t.MaxBudgetCap })
            .FirstOrDefaultAsync();
        var typeCap = type?.MaxBudgetCap ?? 0m;

        // Đơn đặt hàng có thể đặt trần RIÊNG thấp hơn trần loại đề tài (rule #8 — Applied đi theo
        // danh mục đặt hàng). Đơn không đặt trần thì bỏ qua.
        var orderCap = await _cycles.Orders
            .Where(o => o.Id == info.OrderId && o.BudgetCap != null && o.BudgetCap > 0m)
            .Select(o => o.BudgetCap)
            .FirstOrDefaultAsync();

        var caps = new List<decimal>();
        if (typeCap > 0m) caps.Add(typeCap);
        if (orderCap is > 0m) caps.Add(orderCap.Value);

        // Trần có hiệu lực = cái NGHIÊM NGẶT hơn. Chưa cấu hình trần nào ⇒ null = không chặn, chứ
        // không phải chặn ở 0 (đề tài mới tạo sẽ không nộp nổi).
        var effective = caps.Count == 0 ? (decimal?)null : caps.Min();

        return new BudgetCapInfo(type?.Name ?? "", typeCap, orderCap, effective);
    }

    public async Task AssertWithinCapAsync(Guid proposalId, decimal totalAmount)
    {
        var cap = await GetCapAsync(proposalId);
        if (cap.EffectiveCap is not { } limit || totalAmount <= limit) return;

        var source = cap.OrderCap is { } oc && oc == limit
            ? "trần riêng của đơn đặt hàng"
            : $"QĐ543 Điều 14 — {cap.ResearchTypeName}";

        // Định dạng theo vi-VN: 100.000.000đ. Mặc định của server là 100,000,000 — người Việt đọc
        // dấu phẩy là phần thập phân nên số tiền trong thông báo lỗi dễ bị hiểu sai.
        var vi = new CultureInfo("vi-VN");
        throw new ArgumentException(
            $"Tổng dự toán {totalAmount.ToString("N0", vi)}đ vượt trần kinh phí {limit.ToString("N0", vi)}đ ({source}). " +
            $"Hãy giảm dự toán xuống tối đa {limit.ToString("N0", vi)}đ. " +
            "Trường hợp đề tài được Hiệu trưởng đồng ý cấp vượt trần (Điều 14.3), " +
            "đề nghị Phòng Quản lý khoa học điều chỉnh trần của loại đề tài trước khi nộp.");
    }

    public async Task AssertCategoryLimitsAsync(
        IReadOnlyDictionary<int, decimal> amountsByCategoryId, decimal totalAmount)
    {
        // Tỷ lệ tính TRÊN TỔNG dự toán — tổng bằng 0 thì không có gì để chia.
        if (totalAmount <= 0m || amountsByCategoryId.Count == 0) return;

        var ids = amountsByCategoryId.Keys.ToList();
        var categories = await _masterData.BudgetExpenseCategories
            .Where(c => ids.Contains(c.Id) && c.MaxPercentage != null)
            .Select(c => new { c.Id, c.Name, Pct = c.MaxPercentage!.Value })
            .ToListAsync();

        var vi = new CultureInfo("vi-VN");
        var violations = new List<string>();
        foreach (var c in categories.OrderBy(c => c.Pct))
        {
            var amount = amountsByCategoryId[c.Id];
            var limit = Math.Round(totalAmount * c.Pct / 100m, 0);
            if (amount <= limit) continue;

            var actualPct = Math.Round(amount / totalAmount * 100m, 1);
            violations.Add(
                $"\"{c.Name}\" đang chiếm {actualPct.ToString("0.#", vi)}% " +
                $"({amount.ToString("N0", vi)}đ) — tối đa {c.Pct.ToString("0.#", vi)}% " +
                $"tức {limit.ToString("N0", vi)}đ");
        }

        if (violations.Count == 0) return;

        throw new ArgumentException(
            "Dự toán vượt tỷ lệ tối đa theo QĐ543 Điều 15: " + string.Join("; ", violations) +
            $". Tổng dự toán đang là {totalAmount.ToString("N0", vi)}đ — hãy giảm các hạng mục trên " +
            "hoặc phân bổ lại cho cân đối.");
    }
}
