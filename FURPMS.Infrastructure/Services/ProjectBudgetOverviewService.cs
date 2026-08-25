using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Budget;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IProjectBudgetOverviewService"/>
public class ProjectBudgetOverviewService : IProjectBudgetOverviewService
{
    private readonly IProposalRepository _proposals;
    private readonly IContractRepository _contracts;
    private readonly IReviewRepository _review;

    public ProjectBudgetOverviewService(
        IProposalRepository proposals,
        IContractRepository contracts,
        IReviewRepository review)
    {
        _proposals = proposals;
        _contracts = contracts;
        _review = review;
    }

    public async Task<ProjectBudgetOverviewResponse> GetAsync(
        Guid projectId, Guid userId, IEnumerable<string> roles)
    {
        var project = await _proposals.Projects
            .IgnoreQueryFilters()
            .Include(p => p.ResearchType)
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề tài.");

        await AssertCanViewAsync(projectId, project.PiUserId, userId, roles);

        var dto = new ProjectBudgetOverviewResponse
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            TitleVi = project.TitleVi,
            ResearchTypeName = project.ResearchType?.Name,
            // Trần 0 nghĩa là loại đề tài này không đặt trần — trả null để giao diện khỏi vẽ
            // "trần: 0 ₫" rồi báo động đỏ oan.
            FundingCap = project.ResearchType is { MaxBudgetCap: > 0 } rt ? rt.MaxBudgetCap : null
        };

        await FillApprovedBudgetAsync(dto, projectId);
        await FillContractsAndTranchesAsync(dto, projectId);

        dto.RemainingTotal = dto.ContractedTotal - dto.MarkedDisbursedTotal;
        dto.CapExceeded = dto.FundingCap is { } cap && dto.ApprovedTotal > cap;

        return dto;
    }

    /// <summary>
    /// Ai xem được: Phòng QLKH/Quản trị · chủ nhiệm đề tài · thành viên hội đồng được gán chấm đề
    /// tài này (hội đồng nghiệm thu phải đối chiếu kinh phí với sản phẩm — QĐ543 Điều 13.1.e).
    /// </summary>
    private async Task AssertCanViewAsync(
        Guid projectId, Guid piUserId, Guid userId, IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleSet.Contains("Admin") || roleSet.Contains("Staff")) return;
        if (piUserId == userId) return;

        var isCouncilMember = await _review.ProjectAssignments
            .Where(a => a.ProjectId == projectId)
            .AnyAsync(a => _review.CouncilMembers
                .Any(m => m.CouncilId == a.CouncilId && m.UserId == userId));
        if (isCouncilMember) return;

        throw new ForbiddenException("Bạn không có quyền xem kinh phí của đề tài này.");
    }

    private async Task FillApprovedBudgetAsync(ProjectBudgetOverviewResponse dto, Guid projectId)
    {
        // Dự toán bám bản đề cương HIỆN HÀNH: đề cương sửa lại thành v2 thì tiền cũng là của v2.
        var proposalId = await _proposals.Query()
            .IgnoreQueryFilters()
            .Where(p => p.ProjectId == projectId && p.IsCurrent)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync();
        if (proposalId is null) return;

        var budget = await _proposals.Budgets
            .FirstOrDefaultAsync(b => b.ProposalId == proposalId);
        if (budget is not null)
        {
            dto.ApprovedTotal = budget.TotalAmount;

            // 06 hạng mục Điều 15. Giữ nguyên cả hạng mục bằng 0 — hội đồng nhìn cơ cấu cần thấy
            // "mục này không xin đồng nào", khác hẳn với "không có mục này".
            var headings = new (string Code, decimal Amount)[]
            {
                ("LABOR", budget.LaborAmount),
                ("EQUIPMENT", budget.EquipmentAmount),
                ("EXTERNAL_SERVICE", budget.ExternalServiceAmount),
                ("CONFERENCE", budget.ConferenceAmount),
                ("OFFICE", budget.OfficeSuppliesAmount),
                ("INCIDENTAL_IP", budget.IncidentalIpAmount)
            };

            dto.ApprovedByHeading = headings
                .Select(h => new BudgetHeadingDto
                {
                    Code = h.Code,
                    Amount = h.Amount,
                    Percentage = budget.TotalAmount > 0
                        ? Math.Round(h.Amount * 100m / budget.TotalAmount, 1)
                        : 0m
                })
                .ToList();
        }

        dto.ApprovedItems = await _proposals.BudgetItems
            .Where(i => i.ProposalId == proposalId)
            .OrderBy(i => i.Sequence)
            .Select(i => new BudgetItemBreakdownDto
            {
                CategoryName = i.Category.Name,
                Amount = i.Amount,
                SourceKhoan = i.SourceKhoan,
                SourceNgoaiKhoan = i.SourceNgoaiKhoan,
                SourceNsnn = i.SourceNsnn,
                SourceOther = i.SourceOther,
                Note = i.Note
            })
            .ToListAsync();
    }

    private async Task FillContractsAndTranchesAsync(ProjectBudgetOverviewResponse dto, Guid projectId)
    {
        var contracts = await _contracts.Query()
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ContractBriefDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                TotalAmount = c.TotalAmount,
                Status = c.Status,
                SignedAt = c.SignedAt
            })
            .ToListAsync();

        dto.Contracts = contracts;
        dto.ContractedTotal = contracts.Sum(c => c.TotalAmount);
        if (contracts.Count == 0) return;

        var contractIds = contracts.Select(c => c.Id).ToList();

        var tranches = await _contracts.Disbursements
            .Where(d => contractIds.Contains(d.ContractId))
            .Include(d => d.Deliverable)
            .OrderBy(d => d.RoundNumber)
            .ToListAsync();

        dto.Tranches = tranches.Select(d => new DisbursementBriefDto
        {
            Id = d.Id,
            RoundNumber = d.RoundNumber,
            Percentage = d.Percentage,
            PlannedAmount = d.PlannedAmount,
            ActualAmount = d.ActualAmount,
            Status = d.Status,
            ConditionDescription = d.ConditionDescription,
            DisbursedAt = d.DisbursedAt,
            IsBlockedByDeliverable =
                d.Deliverable is not null && d.Deliverable.AcceptanceStatus != AcceptanceStatus.Passed
        }).ToList();

        dto.PlannedTotal = tranches.Sum(d => d.PlannedAmount);

        var disbursed = tranches.Where(d => d.Status == DisbursementStatus.Disbursed).ToList();
        dto.MarkedDisbursedTotal = disbursed.Sum(d => d.ActualAmount ?? d.PlannedAmount);
        dto.HasUnreportedActuals = disbursed.Any(d => d.ActualAmount is null);

        dto.NextTranche = dto.Tranches
            .Where(d => d.Status != DisbursementStatus.Disbursed)
            .OrderBy(d => d.RoundNumber)
            .FirstOrDefault();

        var settlement = await _contracts.Settlements
            .Where(s => contractIds.Contains(s.ContractId))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (settlement is not null)
        {
            dto.Settlement = new SettlementBriefDto
            {
                TotalContractedAmount = settlement.TotalContractedAmount,
                TotalDisbursedAmount = settlement.TotalDisbursedAmount,
                TotalReturnedAmount = settlement.TotalReturnedAmount,
                SettlementDeadline = settlement.SettlementDeadline?.ToString("yyyy-MM-dd"),
                SettlementSignedAt = settlement.SettlementSignedAt
            };
        }
    }
}
