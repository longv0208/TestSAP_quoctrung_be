using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Contract;
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

    public DeliverableService(
        IContractRepository contracts,
        IUserRepository users,
        INotificationRepository notifications)
    {
        _contracts = contracts;
        _users = users;
        _notifications = notifications;
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

    public async Task<DeliverableResponse> SubmitAsync(
        int deliverableId, SubmitDeliverableRequest request, Guid submittedBy)
    {
        var d = await _contracts.Deliverables
            .Include(d => d.Category)
            .FirstOrDefaultAsync(x => x.Id == deliverableId)
            ?? throw new KeyNotFoundException($"Deliverable {deliverableId} not found.");

        d.FileUrl = request.FileUrl;
        d.Description = request.Description ?? d.Description;
        d.SubmittedAt = DateTime.UtcNow;
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
        var fundingMethod = contract.Project.Proposals.FirstOrDefault()?.FundingMethod ?? FundingMethod.Whole;

        if (request.AcceptanceStatus == AcceptanceStatus.Passed && fundingMethod == FundingMethod.Partial)
        {
            var tranche = await _contracts.Disbursements
                .FirstOrDefaultAsync(d => d.DeliverableId == deliverableId);
            if (tranche != null)
            {
                tranche.ConditionMetAt = DateTime.UtcNow;
                tranche.ConditionMetBy = evaluatedBy;
            }

            await NotifyStaffAsync(
                contract,
                "DELIVERABLE_PASSED",
                $"Sản phẩm \"{deliverable.ProductName}\" đã được nghiệm thu. Vui lòng xem xét giải ngân.",
                $"/api/contracts/{contract.Id}/disbursements");
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

            Guid? piUserId = contract.Project?.PiUserId;
            if (piUserId.HasValue)
            {
                await _notifications.AddAsync(new Notification
                {
                    UserId = piUserId.Value,
                    NotificationType = "DELIVERABLE_FAILED",
                    Title = "Sản phẩm không đạt nghiệm thu",
                    Body = $"Sản phẩm \"{deliverable.ProductName}\" không đạt yêu cầu. Hợp đồng đang được xem xét.",
                    ActionUrl = $"/api/contracts/{contract.Id}",
                    RelatedEntityType = "Contract",
                    RelatedEntityId = contract.Id.ToString(),
                    Priority = "HIGH"
                });
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

        foreach (var uid in staffUserIds)
        {
            await _notifications.AddAsync(new Notification
            {
                UserId = uid,
                NotificationType = notificationType,
                Title = notificationType == "DELIVERABLE_PASSED"
                    ? "Điều kiện giải ngân đã đáp ứng"
                    : "Sản phẩm không đạt nghiệm thu",
                Body = body,
                ActionUrl = actionUrl,
                RelatedEntityType = "Contract",
                RelatedEntityId = contract.Id.ToString(),
                Priority = "HIGH"
            });
        }
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
        QualityAssessment = d.QualityAssessment
    };
}
