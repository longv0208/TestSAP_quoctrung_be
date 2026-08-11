using FURPMS.Application.DTOs.Budget;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Proposals;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class ProposalBudgetService : IProposalBudgetService
{
    private readonly IProposalRepository _proposals;
    private readonly IMasterDataRepository _masterData;
    private readonly IBudgetPolicyService _budgetPolicy;

    public ProposalBudgetService(
        IProposalRepository proposals,
        IMasterDataRepository masterData,
        IBudgetPolicyService budgetPolicy)
    {
        _proposals = proposals;
        _masterData = masterData;
        _budgetPolicy = budgetPolicy;
    }

    public async Task<BudgetResponse> GetBudgetAsync(Guid proposalId)
    {
        var budget = await _proposals.Budgets
            .Include(b => b.Proposal)
            .FirstOrDefaultAsync(b => b.ProposalId == proposalId)
            ?? throw new KeyNotFoundException($"Budget for proposal {proposalId} not found.");

        var items = await _proposals.BudgetItems
            .Include(i => i.Category)
            .Where(i => i.ProposalId == proposalId)
            .OrderBy(i => i.Sequence)
            .ToListAsync();

        return MapBudget(budget, items);
    }

    public async Task<BudgetResponse> UpdateBudgetAsync(Guid proposalId, UpdateBudgetRequest request)
    {
        if (request.Items.Count == 0)
            throw new ArgumentException("Phải có ít nhất một khoản dự toán kinh phí.");

        var itemSum = request.Items.Sum(i => i.Amount);
        if (itemSum != request.TotalAmount)
            throw new ArgumentException(
                $"Sum of item amounts ({itemSum:N2}) does not equal totalAmount ({request.TotalAmount:N2}).");

        // QĐ543 Điều 14 — chặn NGAY khi lưu dự toán, đừng để PI điền xong cả đề cương rồi mới báo
        // vượt trần ở bước nộp.
        await _budgetPolicy.AssertWithinCapAsync(proposalId, request.TotalAmount);

        var budget = await _proposals.Budgets
            .FirstOrDefaultAsync(b => b.ProposalId == proposalId)
            ?? throw new KeyNotFoundException($"Budget for proposal {proposalId} not found.");

        var existingItems = await _proposals.BudgetItems
            .Where(i => i.ProposalId == proposalId)
            .ToListAsync();
        _proposals.RemoveBudgetItemsRange(existingItems);

        var seq = 1;
        foreach (var dto in request.Items.OrderBy(i => i.Sequence == 0 ? seq : i.Sequence))
        {
            _proposals.AddBudgetItem(new ProposalBudgetItem
            {
                ProposalId = proposalId,
                CategoryId = dto.CategoryId,
                Amount = dto.Amount,
                SourceKhoan = dto.SourceKhoan,
                SourceNgoaiKhoan = dto.SourceNgoaiKhoan,
                SourceNsnn = dto.SourceNsnn,
                SourceOther = dto.SourceOther,
                Sequence = dto.Sequence > 0 ? dto.Sequence : seq
            });
            seq++;
        }

        budget.TotalAmount = request.TotalAmount;
        await _proposals.SaveChangesAsync();

        return await GetBudgetAsync(proposalId);
    }

    public async Task<IEnumerable<LaborDetailResponse>> GetLaborDetailsAsync(Guid proposalId)
    {
        _ = await _proposals.Budgets.FirstOrDefaultAsync(b => b.ProposalId == proposalId)
            ?? throw new KeyNotFoundException($"Budget for proposal {proposalId} not found.");

        var details = await _proposals.LaborDetails
            .Include(d => d.ProjectMember)
            .Where(d => d.ProposalId == proposalId)
            .OrderBy(d => d.Sequence)
            .ToListAsync();

        return details.Select(MapLabor);
    }

    public async Task<LaborDetailResponse> UpdateLaborDetailAsync(Guid proposalId, int detailId, UpdateLaborDetailRequest request)
    {
        var detail = await _proposals.LaborDetails
            .Include(d => d.ProjectMember)
            .FirstOrDefaultAsync(d => d.Id == detailId && d.ProposalId == proposalId)
            ?? throw new KeyNotFoundException($"Labor detail {detailId} not found for proposal {proposalId}.");

        detail.TotalResearchHours = request.TotalResearchHours;
        detail.HourlyRate = request.HourlyRate;
        detail.WorkDays = request.WorkDays;
        detail.Coefficient = request.Coefficient;
        detail.Sequence = request.Sequence;

        if (request.Coefficient.HasValue)
        {
            var config = await _masterData.SystemFinancialConfigs
                .FirstOrDefaultAsync(c => c.Code == "BASE_DAILY_SALARY" && c.IsActive)
                ?? throw new KeyNotFoundException("System config BASE_DAILY_SALARY not found.");
            detail.DailyRate = request.Coefficient.Value * config.Value;
        }
        else
        {
            detail.DailyRate = null;
        }

        await _proposals.SaveChangesAsync();
        return MapLabor(detail);
    }

    private static BudgetResponse MapBudget(
        ProposalBudget b,
        IEnumerable<ProposalBudgetItem> items) => new()
    {
        BudgetId = b.Id,
        TotalAmount = b.TotalAmount,
        LaborAmount = b.LaborAmount,
        EquipmentAmount = b.EquipmentAmount,
        ExternalServiceAmount = b.ExternalServiceAmount,
        ConferenceAmount = b.ConferenceAmount,
        OfficeSuppliesAmount = b.OfficeSuppliesAmount,
        IncidentalIpAmount = b.IncidentalIpAmount,
        Items = items.Select(i => new BudgetItemDto
        {
            Id = i.Id,
            CategoryId = i.CategoryId,
            CategoryCode = i.Category?.Code,
            CategoryName = i.Category?.Name,
            Amount = i.Amount,
            SourceKhoan = i.SourceKhoan,
            SourceNgoaiKhoan = i.SourceNgoaiKhoan,
            SourceNsnn = i.SourceNsnn,
            SourceOther = i.SourceOther,
            Sequence = i.Sequence
        }).ToList()
    };

    private static LaborDetailResponse MapLabor(ProposalBudgetLaborDetail d) => new()
    {
        Id = d.Id,
        TeamMemberId = d.ProjectMemberId,
        TeamMemberName = d.ProjectMember?.FullName,
        TotalResearchHours = d.TotalResearchHours,
        HourlyRate = d.HourlyRate,
        TotalAmount = d.TotalAmount,
        WorkDays = d.WorkDays,
        Coefficient = d.Coefficient,
        DailyRate = d.DailyRate,
        ComputedDailyTotal = d.WorkDays.HasValue && d.DailyRate.HasValue
            ? d.WorkDays.Value * d.DailyRate.Value
            : null,
        Sequence = d.Sequence
    };
}
