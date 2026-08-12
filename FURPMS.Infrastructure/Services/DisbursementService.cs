using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class DisbursementService : IDisbursementService
{
    private readonly IContractRepository _contracts;
    private readonly IMasterDataRepository _masterData;
    private readonly IClock _clock;
    private readonly ISystemSettingService _settings;

    public DisbursementService(IContractRepository contracts, IMasterDataRepository masterData,
        IClock clock,
        ISystemSettingService settings)
    {
        _contracts = contracts;
        _masterData = masterData;
        _clock = clock;
        _settings = settings;
    }

    public async Task<IEnumerable<DisbursementResponse>> GetByContractAsync(Guid contractId)
    {
        _ = await _contracts.Query().FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        var items = await _contracts.Disbursements
            .Include(d => d.Deliverable)
            .Where(d => d.ContractId == contractId)
            .OrderBy(d => d.RoundNumber)
            .ToListAsync();

        return items.Select(Map);
    }

    public async Task<DisbursementResponse> LinkDeliverableAsync(int disbursementId, LinkDeliverableRequest request)
    {
        var d = await _contracts.Disbursements
            .Include(x => x.Deliverable)
            .FirstOrDefaultAsync(x => x.Id == disbursementId)
            ?? throw new KeyNotFoundException($"Disbursement {disbursementId} not found.");

        if (d.Status == DisbursementStatus.Disbursed)
            throw new InvalidOperationException(
                "Đợt này đã đánh dấu giải ngân — không đổi được sản phẩm minh chứng nữa.");

        if (request.DeliverableId is int deliverableId)
        {
            // Chỉ cho gắn sản phẩm CÙNG hợp đồng, tránh lấy minh chứng của đề tài khác.
            var deliverable = await _contracts.Deliverables
                .FirstOrDefaultAsync(x => x.Id == deliverableId && x.ContractId == d.ContractId)
                ?? throw new ArgumentException(
                    $"Sản phẩm {deliverableId} không thuộc hợp đồng của đợt giải ngân này.");

            d.DeliverableId = deliverable.Id;
            d.Deliverable = deliverable;

            // Sản phẩm đã nghiệm thu từ trước ⇒ điều kiện coi như đạt ngay khi gắn.
            d.ConditionMetAt = deliverable.AcceptanceStatus == AcceptanceStatus.Passed
                ? d.ConditionMetAt ?? _clock.UtcNow
                : null;
        }
        else
        {
            d.DeliverableId = null;
            d.Deliverable = null;
            d.ConditionMetAt = null;
        }

        await _contracts.SaveChangesAsync();
        return Map(d);
    }

    public async Task<IEnumerable<DisbursementResponse>> GenerateAsync(Guid contractId)
    {
        var contract = await _contracts.Query()
            .Include(c => c.Project).ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .Include(c => c.Disbursements)
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (contract.Disbursements.Any())
            throw new InvalidOperationException("Hợp đồng này đã sinh các đợt giải ngân rồi.");

        // QĐ543 **Điều 16**: lịch giải ngân do **LOẠI ĐỀ TÀI** quyết định, KHÔNG phải lựa chọn của
        // chủ nhiệm. Ứng dụng 4 đợt 30–30–30–10; Cơ bản 1 lần sau nghiệm thu "Đạt".
        //
        // Trước đây code chia theo `Proposal.FundingMethod` (WHOLE/PARTIAL) — khái niệm "phương
        // thức khoán chi" lấy từ mẫu thuyết minh cấp Bộ, **không có trong QĐ543** (rà toàn văn:
        // chữ "khoán" chỉ xuất hiện ở "thuê khoán chuyên môn" và "giao khoán", không có mục nào
        // cho chủ nhiệm chọn kiểu giải ngân). Chia sai gốc thì số đợt và điều kiện mở đều lệch.
        var tranches = await GenerateFromTemplateAsync(contract);

        _contracts.AddDisbursementsRange(tranches);
        await _contracts.SaveChangesAsync();

        return tranches.Select(Map);
    }

    public async Task<DisbursementResponse> ConfirmAsync(int disbursementId, ConfirmDisbursementRequest request, Guid processedBy)
    {
        var d = await _contracts.Disbursements
            .Include(x => x.Deliverable)
            .FirstOrDefaultAsync(x => x.Id == disbursementId)
            ?? throw new KeyNotFoundException($"Disbursement {disbursementId} not found.");

        if (d.Status == DisbursementStatus.Disbursed)
            throw new InvalidOperationException("Đợt giải ngân này đã được đánh dấu đã chi.");

        // Đợt nào có gắn sản phẩm minh chứng thì sản phẩm phải nghiệm thu ĐẠT rồi mới
        // được đánh dấu đã giải ngân (QĐ543 Điều 16 — giải ngân theo tiến độ thực hiện).
        // Đợt KHÔNG gắn sản phẩm (vd tạm ứng khởi động hợp đồng) vẫn cho qua bình thường.
        if (d.Deliverable is not null && d.Deliverable.AcceptanceStatus != AcceptanceStatus.Passed)
            throw new InvalidOperationException(
                $"Sản phẩm minh chứng \"{d.Deliverable.ProductName}\" chưa nghiệm thu Đạt " +
                "— chưa thể đánh dấu đã giải ngân đợt này.");

        await AssertFinalTrancheUnlockedAsync(d);

        if (request.ActualAmount.HasValue) d.ActualAmount = request.ActualAmount;   // optional, không bắt buộc
        if (!string.IsNullOrWhiteSpace(request.BankReference)) d.BankReference = request.BankReference;
        d.Notes = request.Notes;
        d.Status = DisbursementStatus.Disbursed;
        d.DisbursedAt = _clock.UtcNow;
        d.ProcessedBy = processedBy;

        await _contracts.SaveChangesAsync();
        return Map(d);
    }

    /// <summary>
    /// QĐ543 **BM05 Điều 4.2**: *"Đợt cuối: giải ngân kinh phí còn lại **sau khi đề tài được công
    /// nhận kết quả Đạt**"*. Trước đây đợt cuối chi được bất cứ lúc nào, nên có thể chi hết tiền
    /// rồi mới họp nghiệm thu — mất luôn đòn bẩy cuối cùng của mốc giải ngân.
    /// <para>
    /// Mốc "công nhận Đạt" đọc qua <c>Project.Status == COMPLETED</c> — trạng thái này CHỈ được đặt
    /// ở một đường duy nhất: Chủ tịch chốt biên bản vòng NGHIỆM THU với kết quả Đạt
    /// (<c>ReviewScoringService.ApproveMinutesAsync</c>).
    /// </para>
    /// </summary>
    private async Task AssertFinalTrancheUnlockedAsync(ContractDisbursement d)
    {
        // Hợp đồng chỉ có MỘT đợt thì đợt đó vừa là đầu vừa là cuối — chặn nó là cấm luôn khoản
        // tạm ứng sau khi ký, tức là đề tài không có tiền để bắt đầu. "Đợt cuối" theo BM05 chỉ có
        // nghĩa khi hợp đồng chia thành nhiều đợt.
        var tranches = await _contracts.Disbursements
            .Where(x => x.ContractId == d.ContractId)
            .Select(x => x.RoundNumber)
            .ToListAsync();
        if (tranches.Count < 2) return;
        if (tranches.Any(n => n > d.RoundNumber)) return;   // chưa phải đợt cuối

        var project = await _contracts.Query()
            .Where(c => c.Id == d.ContractId)
            .Select(c => new { c.Project.Status, c.Project.TitleVi })
            .FirstOrDefaultAsync();
        if (project == null || project.Status == ProjectStatus.Completed) return;

        throw new InvalidOperationException(
            $"Đợt {d.RoundNumber} là đợt giải ngân CUỐI — theo QĐ543 (BM05 Điều 4.2) chỉ được chi " +
            "kinh phí còn lại sau khi đề tài được hội đồng nghiệm thu công nhận kết quả Đạt. " +
            $"Đề tài đang ở trạng thái \"{StatusText.Vi(project.Status)}\", chưa có kết luận nghiệm thu Đạt.");
    }

    /// <summary>
    /// Sinh đợt giải ngân từ bảng mốc chuẩn của **loại đề tài** (QĐ543 Điều 16).
    /// Chưa cấu hình mốc cho loại đó thì lùi về một đợt 100% sau nghiệm thu — thà ít đợt còn hơn
    /// bịa ra lịch không có căn cứ.
    /// </summary>
    private async Task<List<ContractDisbursement>> GenerateFromTemplateAsync(
        Domain.Entities.Contracts.Contract contract)
    {
        var templates = await _masterData.DisbursementTemplates
            .Where(t => t.ResearchTypeId == contract.Project.ResearchTypeId && t.IsActive)
            .OrderBy(t => t.RoundNumber)
            .ToListAsync();

        if (templates.Count == 0)
        {
            return new List<ContractDisbursement>
            {
                new()
                {
                    ContractId = contract.Id,
                    RoundNumber = 1,
                    Percentage = 100m,
                    PlannedAmount = contract.TotalAmount,
                    ConditionDescription = "Sau khi Hội đồng nghiệm thu đánh giá \"Đạt\"",
                    Status = DisbursementStatus.Pending
                }
            };
        }

        var list = new List<ContractDisbursement>();
        decimal allocated = 0;
        for (int i = 0; i < templates.Count; i++)
        {
            var t = templates[i];
            // Đợt cuối lấy phần CÒN LẠI để tổng luôn khớp giá trị hợp đồng, tránh lệch vài đồng
            // do làm tròn từng đợt.
            var amount = i == templates.Count - 1
                ? contract.TotalAmount - allocated
                : Math.Round(contract.TotalAmount * t.Percentage / 100m, 0);
            allocated += amount;

            list.Add(new ContractDisbursement
            {
                ContractId = contract.Id,
                RoundNumber = t.RoundNumber,
                Percentage = t.Percentage,
                PlannedAmount = amount,
                ConditionDescription = t.ConditionDescription,
                Status = DisbursementStatus.Pending
            });
        }
        return list;
    }

    private static DisbursementResponse Map(ContractDisbursement d) => new()
    {
        Id = d.Id,
        ContractId = d.ContractId,
        RoundNumber = d.RoundNumber,
        Percentage = d.Percentage,
        PlannedAmount = d.PlannedAmount,
        ActualAmount = d.ActualAmount,
        ConditionDescription = d.ConditionDescription,
        ConditionMetAt = d.ConditionMetAt,
        DisbursedAt = d.DisbursedAt,
        BankReference = d.BankReference,
        Status = d.Status,
        Notes = d.Notes,
        DeliverableId = d.DeliverableId,
        DeliverableName = d.Deliverable?.ProductName,
        DeliverableAcceptanceStatus = d.Deliverable?.AcceptanceStatus,
        DeliverableSubmittedAt = d.Deliverable?.SubmittedAt,
        IsBlockedByDeliverable =
            d.Deliverable is not null && d.Deliverable.AcceptanceStatus != AcceptanceStatus.Passed
    };
}
