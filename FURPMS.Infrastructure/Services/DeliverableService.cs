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

    public DeliverableService(
        IContractRepository contracts,
        IUserRepository users,
        INotificationRepository notifications,
        INotifier notifier,
        IClock clock)
    {
        _contracts = contracts;
        _users = users;
        _notifications = notifications;
        _notifier = notifier;
        _clock = clock;
    }

    public async Task<IEnumerable<DeliverableResponse>> GetByContractAsync(Guid contractId)
    {
        _ = await _contracts.Query().FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

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
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");
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

    public async Task<DeliverableResponse> SubmitAsync(
        int deliverableId, SubmitDeliverableRequest request, Guid submittedBy)
    {
        var d = await _contracts.Deliverables
            .Include(d => d.Category)
            .FirstOrDefaultAsync(x => x.Id == deliverableId)
            ?? throw new KeyNotFoundException($"Deliverable {deliverableId} not found.");

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
            throw new ArgumentException("AcceptanceStatus must be PASSED or FAILED.");

        var deliverable = await _contracts.Deliverables
            .Include(d => d.Category)
            .Include(d => d.Contract!)
                .ThenInclude(c => c.Project)
                    .ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .FirstOrDefaultAsync(x => x.Id == deliverableId)
            ?? throw new KeyNotFoundException($"Deliverable {deliverableId} not found.");

        if (deliverable.Contract == null)
            throw new InvalidOperationException("Deliverable chưa được gắn vào hợp đồng nào — không thể nghiệm thu theo hợp đồng.");

        deliverable.AcceptanceStatus = request.AcceptanceStatus;
        deliverable.QualityAssessment = request.QualityAssessment;
        deliverable.IsCompleted = request.AcceptanceStatus == AcceptanceStatus.Passed;

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
                    $"/contracts/{contract.Id}");
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
                $"/api/contracts/{contract.Id}");

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

    private async Task NotifyStaffAsync(
        Domain.Entities.Contracts.Contract contract,
        string notificationType,
        string body,
        string actionUrl)
    {
        var staffUserIds = await _users.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Name == "Staff")
            .Select(ur => ur.UserId)
            .ToListAsync();

        await _notifier.NotifyManyAsync(
            staffUserIds,
            notificationType,
            notificationType == "DELIVERABLE_PASSED"
                ? "Điều kiện giải ngân đã đáp ứng"
                : "Sản phẩm không đạt nghiệm thu",
            body,
            actionUrl: actionUrl,
            entityType: "Contract",
            entityId: contract.Id.ToString(),
            priority: "HIGH");
    }

    private static DeliverableResponse Map(Domain.Entities.Projects.ProjectDeliverable d) => new()
    {
        Id = d.Id,
        ProjectId = d.ProjectId,
        ContractId = d.ContractId,
        CategoryId = d.CategoryId,
        CategoryName = d.Category?.Name,
        ProductName = d.ProductName,
        Description = d.Description,
        DueDate = d.DueDate,
        AcceptanceStatus = d.AcceptanceStatus,
        IsCompleted = d.IsCompleted,
        SubmittedAt = d.SubmittedAt,
        FileUrl = d.FileUrl,
        TrialEvidenceUrl = d.TrialEvidenceUrl,
        QualityAssessment = d.QualityAssessment
    };
}
