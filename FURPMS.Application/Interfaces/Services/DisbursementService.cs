using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class DisbursementService : IDisbursementService
{
    private readonly IContractRepository _contracts;
    private readonly IMasterDataRepository _masterData;

    public DisbursementService(IContractRepository contracts, IMasterDataRepository masterData)
    {
        _contracts = contracts;
        _masterData = masterData;
    }

    public async Task<IEnumerable<DisbursementResponse>> GetByContractAsync(Guid contractId)
    {
        _ = await _contracts.Query().FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        var items = await _contracts.Disbursements
            .Where(d => d.ContractId == contractId)
            .OrderBy(d => d.RoundNumber)
            .ToListAsync();

        return items.Select(Map);
    }

    public async Task<IEnumerable<DisbursementResponse>> GenerateAsync(Guid contractId)
    {
        var contract = await _contracts.Query()
            .Include(c => c.Project).ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .Include(c => c.Disbursements)
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (contract.Disbursements.Any())
            throw new InvalidOperationException("Disbursement tranches already generated for this contract.");

        var fundingMethod = contract.Project?.Proposals.FirstOrDefault()?.FundingMethod ?? FundingMethod.Whole;

        List<ContractDisbursement> tranches;

        if (fundingMethod == FundingMethod.Partial)
            tranches = await GeneratePartialAsync(contract);
        else
            tranches = await GenerateWholeAsync(contract);

        _contracts.AddDisbursementsRange(tranches);
        await _contracts.SaveChangesAsync();

        return tranches.Select(Map);
    }

    public async Task<DisbursementResponse> ConfirmAsync(int disbursementId, ConfirmDisbursementRequest request, Guid processedBy)
    {
        var d = await _contracts.Disbursements.FirstOrDefaultAsync(x => x.Id == disbursementId)
            ?? throw new KeyNotFoundException($"Disbursement {disbursementId} not found.");

        if (d.Status == DisbursementStatus.Disbursed)
            throw new InvalidOperationException("Disbursement already confirmed.");

        d.ActualAmount = request.ActualAmount;
        d.BankReference = request.BankReference;
        d.Notes = request.Notes;
        d.Status = DisbursementStatus.Disbursed;
        d.DisbursedAt = DateTime.UtcNow;
        d.ProcessedBy = processedBy;

        await _contracts.SaveChangesAsync();
        return Map(d);
    }

    private async Task<List<ContractDisbursement>> GenerateWholeAsync(Domain.Entities.Contracts.Contract contract)
    {
        const int count = 3;
        var templates = await _masterData.DisbursementTemplates
            .Where(t => t.ResearchTypeId == contract.Project.ResearchTypeId && t.IsActive)
            .OrderBy(t => t.RoundNumber)
            .Take(count)
            .ToListAsync();

        var percentages = ResolvePercentages(templates.Select(t => t.Percentage).ToList(), count);
        var descriptions = new[] { "Khởi động hợp đồng", "Giữa hợp đồng", "Kết thúc hợp đồng" };
        var conditionsFromTemplate = templates.Select(t => t.ConditionDescription).ToArray();

        var list = new List<ContractDisbursement>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new ContractDisbursement
            {
                ContractId = contract.Id,
                RoundNumber = i + 1,
                Percentage = percentages[i],
                PlannedAmount = Math.Round(contract.TotalAmount * percentages[i] / 100m, 2),
                ConditionDescription = conditionsFromTemplate.Length > i
                    ? conditionsFromTemplate[i]
                    : descriptions[i],
                Status = DisbursementStatus.Pending
            });
        }
        return list;
    }

    private async Task<List<ContractDisbursement>> GeneratePartialAsync(Domain.Entities.Contracts.Contract contract)
    {
        var deliverables = await _contracts.Deliverables
            .Where(d => d.ContractId == contract.Id)
            .OrderBy(d => d.Id)
            .ToListAsync();

        if (!deliverables.Any())
            throw new InvalidOperationException(
                "No deliverables found for this contract. Cannot generate PARTIAL tranches.");

        int count = deliverables.Count;
        var templates = await _masterData.DisbursementTemplates
            .Where(t => t.ResearchTypeId == contract.Project.ResearchTypeId && t.IsActive)
            .OrderBy(t => t.RoundNumber)
            .Take(count)
            .ToListAsync();

        var percentages = ResolvePercentages(templates.Select(t => t.Percentage).ToList(), count);

        var list = new List<ContractDisbursement>();
        for (int i = 0; i < count; i++)
        {
            var deliverable = deliverables[i];
            var condDesc = templates.Count > i
                ? templates[i].ConditionDescription
                : $"Nghiệm thu: {deliverable.ProductName}";

            list.Add(new ContractDisbursement
            {
                ContractId = contract.Id,
                RoundNumber = i + 1,
                Percentage = percentages[i],
                PlannedAmount = Math.Round(contract.TotalAmount * percentages[i] / 100m, 2),
                ConditionDescription = condDesc,
                DeliverableId = deliverable.Id,
                Status = DisbursementStatus.Pending
            });
        }
        return list;
    }

    private static List<decimal> ResolvePercentages(List<decimal> templatePcts, int count)
    {
        if (templatePcts.Count == count && Math.Abs((double)templatePcts.Sum() - 100.0) < 0.01)
            return templatePcts;

        var baseValue = Math.Floor(100m / count);
        var list = Enumerable.Repeat(baseValue, count - 1).ToList();
        list.Add(100m - list.Sum());
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
        DeliverableId = d.DeliverableId
    };
}
