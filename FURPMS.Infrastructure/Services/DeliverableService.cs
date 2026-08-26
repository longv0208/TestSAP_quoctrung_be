using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class DeliverableService : IDeliverableService
{
    private readonly IContractRepository _contracts;
    private readonly IUserRepository _users;
    private readonly INotificationRepository _notifications;
    private readonly INotifier _notifier;
    private readonly IClock _clock;
    private readonly IDecisionLogger _decisions;

    public DeliverableService(
        IContractRepository contracts,
        IUserRepository users,
        INotificationRepository notifications,
        INotifier notifier,
        IClock clock,
        IDecisionLogger decisions)
    {
        _decisions = decisions;
        _contracts = contracts;
        _users = users;
        _notifications = notifications;
        _notifier = notifier;
        _clock = clock;
    }

    public async Task<IEnumerable<DeliverableResponse>> GetByContractAsync(Guid contractId)
    {
        _ = await _contracts.Query().FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        var items = await _contracts.Deliverables
            .Include(d => d.Category)
            .Where(d => d.ContractId == contractId)
            .OrderBy(d => d.Id)
            .ToListAsync();

        return items.Select(Map);
    }

    // Staff thêm 1 sản phẩm phải nộp cho hợp đồng (đề cương không có trường sản phẩm cấu trúc).
    public async Task<DeliverableResponse> CreateAsync(Guid contractId, CreateDeliverableRequest request, Guid createdBy)
    {
        var contract = await _contracts.Query().FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (string.IsNullOrWhiteSpace(request.ProductName))
            throw new ArgumentException("Tên sản phẩm là bắt buộc.");

        DateOnly? due = null;
        if (!string.IsNullOrWhiteSpace(request.DueDate))
        {
            if (!DateOnly.TryParse(request.DueDate, out var d))
                throw new ArgumentException("DueDate phải là ngày hợp lệ (yyyy-MM-dd).");
            due = d;
        }

        var maxSeq = await _contracts.Deliverables
            .Where(d => d.ContractId == contractId)
            .Select(d => (int?)d.Sequence).MaxAsync() ?? 0;

        var deliverable = new Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = contract.ProjectId,
            ContractId = contractId,
            ProductName = request.ProductName.Trim(),
            CategoryId = request.CategoryId,
            Description = request.Description,
            DueDate = due,
            Sequence = maxSeq + 1
        };
        _contracts.AddDeliverable(deliverable);
        await _contracts.SaveChangesAsync();

        var saved = await _contracts.Deliverables.Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == deliverable.Id);
        return Map(saved ?? deliverable);
    }

    /// <summary>
    /// Chỉ nhận đường dẫn nội bộ (BE sinh sau khi upload, bắt đầu bằng "/") hoặc link http(s) đầy đủ.
    /// Trước đây nhận nguyên xi mọi chuỗi ⇒ PI gõ "abc.com" vẫn lưu được, nhưng thiếu scheme thì
    /// trình duyệt hiểu là đường dẫn TƯƠNG ĐỐI: người nghiệm thu bấm vào chỉ ra trang trống,
    /// tưởng là chưa có sản phẩm.
    /// </summary>
    private static string NormalizeFileUrl(string? raw, string label)
    {
        var url = raw?.Trim() ?? "";
        if (url.Length == 0)
            throw new ArgumentException($"Chưa có đường dẫn {label}.");
        if (url.StartsWith('/'))
            return url;
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException($"Đường dẫn {label} không hợp lệ: \"{raw}\".");
        return url;
    }

    /// <summary>
    /// Staff sửa mô tả/hạn/loại sản phẩm. Sản phẩm đã nghiệm thu ĐẠT thì khoá — sửa tên một sản
    /// phẩm đã được hội đồng thông qua là làm sai lệch chính cái hội đồng đã duyệt.
    /// </summary>
    public async Task<DeliverableResponse> UpdateAsync(int deliverableId, CreateDeliverableRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName))
            throw new ArgumentException("Phải nhập tên sản phẩm.");

        var d = await _contracts.Deliverables
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == deliverableId)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm {deliverableId}.");

        if (d.AcceptanceStatus == AcceptanceStatus.Passed)
            throw new InvalidOperationException(
                $"Sản phẩm \"{d.ProductName}\" đã nghiệm thu ĐẠT — không sửa được nữa.");

        d.ProductName = request.ProductName.Trim();
        d.Description = request.Description;
        if (request.CategoryId.HasValue) d.CategoryId = request.CategoryId;
        if (!string.IsNullOrWhiteSpace(request.DueDate))
        {
            if (!DateOnly.TryParse(request.DueDate, out var due))
                throw new ArgumentException("Hạn nộp sản phẩm không hợp lệ (định dạng yyyy-MM-dd).");
            d.DueDate = due;
        }

        await _contracts.SaveChangesAsync();
        return Map(d);
    }

    /// <summary>
    /// Xoá sản phẩm khỏi hợp đồng. Ba cửa khoá, theo thứ tự hậu quả nặng dần:
    /// đã nộp minh chứng · đã nghiệm thu · đang là điều kiện của một đợt giải ngân.
    /// Cái cuối nguy nhất: xoá đi là đợt giải ngân mất căn cứ mở, không ai truy lại được vì sao.
    /// </summary>
    public async Task DeleteAsync(int deliverableId)
    {
        var d = await _contracts.Deliverables
            .FirstOrDefaultAsync(x => x.Id == deliverableId)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm {deliverableId}.");

        if (d.AcceptanceStatus == AcceptanceStatus.Passed)
            throw new InvalidOperationException(
                $"Sản phẩm \"{d.ProductName}\" đã nghiệm thu ĐẠT — không xoá được.");

        if (d.SubmittedAt != null)
            throw new InvalidOperationException(
                $"Sản phẩm \"{d.ProductName}\" đã được nộp minh chứng — không xoá được. " +
                "Nếu nộp nhầm, đánh giá Không đạt để chủ nhiệm nộp lại.");

        var linkedTranche = await _contracts.Disbursements
            .Where(x => x.DeliverableId == deliverableId)
            .Select(x => (int?)x.RoundNumber)
            .FirstOrDefaultAsync();
        if (linkedTranche.HasValue)
            throw new InvalidOperationException(
                $"Sản phẩm \"{d.ProductName}\" đang là minh chứng của đợt giải ngân {linkedTranche} " +
                "— gỡ khỏi đợt đó trước rồi mới xoá được.");

        _contracts.RemoveDeliverable(d);
        await _contracts.SaveChangesAsync();
    }

    public async Task<DeliverableResponse> SubmitAsync(
        int deliverableId, SubmitDeliverableRequest request, Guid submittedBy)
    {
        var d = await _contracts.Deliverables
            .Include(d => d.Category)
            .FirstOrDefaultAsync(x => x.Id == deliverableId)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

        /*
         * Sản phẩm đã nghiệm thu ĐẠT thì ĐÓNG, không nộp lại được nữa
         * (thầy 05/08: "từng đợt của sản phẩm sau khi xong thì đều phải đóng lại hết").
         *
         * Trước đây không kiểm gì: nộp lại một sản phẩm đã Đạt sẽ đặt lại AcceptanceStatus về
         * PENDING ở ngay dưới ⇒ **xoá mất kết quả nghiệm thu**, và khoá lại đợt giải ngân vốn
         * đã được mở nhờ sản phẩm đó. FE có ẩn nút nhưng gọi thẳng API là lọt.
         */
        if (d.AcceptanceStatus == AcceptanceStatus.Passed)
            throw new InvalidOperationException(
                $"Sản phẩm \"{d.ProductName}\" đã nghiệm thu ĐẠT — không nộp lại được. " +
                "Nếu cần thay bản khác, liên hệ phòng QLKH để mở lại.");

        d.FileUrl = NormalizeFileUrl(request.FileUrl, "sản phẩm");
        d.Description = request.Description ?? d.Description;
        // Chỉ ghi đè khi PI thật sự nộp minh chứng mới — nộp lại mà bỏ trống thì giữ bản cũ.
        if (!string.IsNullOrWhiteSpace(request.TrialEvidenceUrl))
            d.TrialEvidenceUrl = NormalizeFileUrl(request.TrialEvidenceUrl, "minh chứng thử nghiệm");
        d.SubmittedAt = _clock.UtcNow;
        d.AcceptanceStatus = AcceptanceStatus.Pending;
        await _contracts.SaveChangesAsync();

        return Map(d);
    }

    public async Task<DeliverableResponse> EvaluateAsync(
        int deliverableId, EvaluateDeliverableRequest request, Guid evaluatedBy)
    {
        if (request.AcceptanceStatus is not (AcceptanceStatus.Passed or AcceptanceStatus.Failed))
            throw new ArgumentException("Kết quả nghiệm thu sản phẩm chỉ nhận Đạt (PASSED) hoặc Không đạt (FAILED).");

        var deliverable = await _contracts.Deliverables
            .Include(d => d.Category)
            .Include(d => d.Contract!)
                .ThenInclude(c => c.Project)
                    .ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .FirstOrDefaultAsync(x => x.Id == deliverableId)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

        if (deliverable.Contract == null)
            throw new InvalidOperationException("Deliverable chưa được gắn vào hợp đồng nào — không thể nghiệm thu theo hợp đồng.");

        deliverable.AcceptanceStatus = request.AcceptanceStatus;
        deliverable.QualityAssessment = request.QualityAssessment;
        deliverable.IsCompleted = request.AcceptanceStatus == AcceptanceStatus.Passed;

        _decisions.Log(
            deliverable.ProjectId, DecisionTypes.DeliverableAccepted,
            $"Nghiệm thu sản phẩm \"{deliverable.ProductName}\": " +
                (request.AcceptanceStatus == AcceptanceStatus.Passed ? "Đạt" : "Không đạt"),
            "ProjectDeliverable", deliverable.Id.ToString(),
            result: request.AcceptanceStatus, reason: request.QualityAssessment,
            decidedBy: evaluatedBy, decidedByRole: "Hội đồng nghiệm thu");

        var contract = deliverable.Contract;

        if (request.AcceptanceStatus == AcceptanceStatus.Passed)
        {
            // Mở khoá điều kiện giải ngân theo LIÊN KẾT đợt↔sản phẩm, không theo
            // fundingMethod nữa: Staff nay gắn được sản phẩm cho cả đợt WHOLE (P5).
            // Lấy TẤT CẢ đợt trỏ tới sản phẩm này — 1 sản phẩm có thể là điều kiện
            // của nhiều đợt.
            var tranches = await _contracts.Disbursements
                .Where(d => d.DeliverableId == deliverableId)
                .ToListAsync();

            foreach (var tranche in tranches)
            {
                tranche.ConditionMetAt = _clock.UtcNow;
                tranche.ConditionMetBy = evaluatedBy;
            }

            if (tranches.Count > 0)
            {
                await NotifyStaffAsync(
                    contract,
                    "DELIVERABLE_PASSED",
                    $"Sản phẩm \"{deliverable.ProductName}\" đã được nghiệm thu. Vui lòng xem xét giải ngân.",
                    // Không có route chi tiết /contracts/{id} riêng ở FE (mở hợp đồng là sheet trên
                    // trang danh sách) — trỏ về danh sách thay vì một đường không route nào khớp.
                    "/contracts");
            }

            // PI phải biết sản phẩm mình nộp đã ĐẠT — trước đây chỉ báo khi KHÔNG đạt,
            // nộp xong đạt thì im lặng, PI không biết đã xong hay chưa.
            if (contract.Project?.PiUserId is Guid passedPiUserId)
            {
                await _notifier.NotifyAsync(
                    passedPiUserId,
                    "DELIVERABLE_PASSED",
                    "Sản phẩm đã được nghiệm thu",
                    $"Sản phẩm \"{deliverable.ProductName}\" đã được nghiệm thu ĐẠT.",
                    actionUrl: "/deliverables",
                    entityType: "Contract",
                    entityId: contract.Id.ToString());
            }
        }
        else if (request.AcceptanceStatus == AcceptanceStatus.Failed)
        {
            contract.Status = ContractStatus.UnderReview;
            contract.UpdatedAt = DateTime.UtcNow;

            await NotifyStaffAsync(
                contract,
                "DELIVERABLE_FAILED",
                $"Sản phẩm \"{deliverable.ProductName}\" không đạt nghiệm thu. Hợp đồng cần xem xét lại.",
                "/contracts");

            if (contract.Project?.PiUserId is Guid failedPiUserId)
            {
                await _notifier.NotifyAsync(
                    failedPiUserId,
                    "DELIVERABLE_FAILED",
                    "Sản phẩm không đạt nghiệm thu",
                    $"Sản phẩm \"{deliverable.ProductName}\" không đạt yêu cầu. Hợp đồng đang được xem xét.",
                    actionUrl: "/deliverables",
                    entityType: "Contract",
                    entityId: contract.Id.ToString(),
                    priority: "HIGH");
            }
        }

        await _contracts.SaveChangesAsync();
        return Map(deliverable);
    }

    /// <summary>Báo cho toàn bộ Phòng QLKH về một mốc của hợp đồng.</summary>
    private Task NotifyStaffAsync(
        Domain.Entities.Contracts.Contract contract,
        string notificationType,
        string body,
        string actionUrl)
        => _notifier.NotifyRoleAsync(
            "Staff",
            notificationType,
            notificationType == "DELIVERABLE_PASSED"
                ? "Điều kiện giải ngân đã đáp ứng"
                : "Sản phẩm không đạt nghiệm thu",
            body,
            actionUrl: actionUrl,
            entityType: "Contract",
            entityId: contract.Id.ToString(),
            priority: "HIGH");

    private DeliverableResponse Map(Domain.Entities.Projects.ProjectDeliverable d) => new()
    {
        Id = d.Id,
        ProjectId = d.ProjectId,
        ContractId = d.ContractId,
        CategoryId = d.CategoryId,
        CategoryName = d.Category?.Name,
        ProductName = d.ProductName,
        Description = d.Description,
        ScientificRequirements = d.ScientificRequirements,
        Notes = d.Notes,
        DueDate = d.DueDate,
        DaysLeft = DeadlineMath.DaysLeft(d.DueDate, _clock),
        AcceptanceStatus = d.AcceptanceStatus,
        IsCompleted = d.IsCompleted,
        SubmittedAt = d.SubmittedAt,
        FileUrl = d.FileUrl,
        TrialEvidenceUrl = d.TrialEvidenceUrl,
        QualityAssessment = d.QualityAssessment
    };
}
