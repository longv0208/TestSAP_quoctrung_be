using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Sau Review 2 (điểm e): contract thuộc PROJECT — 1 project có thể ký NHIỀU hợp đồng
// (từng phần/giai đoạn). API vẫn nhận proposalId (FE giữ nguyên) → resolve project.
public class ContractService : IContractService
{
    private readonly IContractRepository _contracts;
    private readonly IProposalRepository _proposals;
    private readonly IClock _clock;
    private readonly ISystemSettingService _settings;

    public ContractService(IContractRepository contracts, IProposalRepository proposals,
        IClock clock,
        ISystemSettingService settings)
    {
        _contracts = contracts;
        _proposals = proposals;
        _clock = clock;
        _settings = settings;
    }

    private IQueryable<Contract> QueryWithProject() => _contracts.Query()
        .Include(c => c.Project)
            .ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
        .Include(c => c.Project)
            .ThenInclude(p => p.PiUser);

    public async Task<IEnumerable<ContractListResponse>> GetListAsync(Guid? piUserId = null)
    {
        var query = QueryWithProject().AsQueryable();

        if (piUserId.HasValue)
            query = query.Where(c => c.Project.PiUserId == piUserId.Value);

        var contracts = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return contracts.Select(MapList);
    }

    public async Task<ContractDetailResponse> GetByIdAsync(Guid contractId)
    {
        var c = await QueryWithProject()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");
        return MapDetail(c);
    }

    public async Task<ContractDetailResponse> CreateAsync(CreateContractRequest request, Guid createdBy)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Budget)
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId)
            ?? throw new KeyNotFoundException($"Proposal {request.ProposalId} not found.");

        if (proposal.Status != ProposalStatus.Approved)
            throw new InvalidOperationException(
                $"Cannot create contract: proposal status is '{proposal.Status}', expected APPROVED.");

        var project = proposal.Project;
        var totalAmount = proposal.Budget?.TotalAmount ?? 0m;

        var sideA = await _settings.GetStringAsync(
            SystemSettingKeys.ContractSideARepresentative,
            SystemSettingKeys.DefaultContractSideARepresentative);

        var contract = new Contract
        {
            ProjectId = project.Id,
            ContractNumber = request.ContractNumber,
            ScopeTitle = request.ScopeTitle,
            TotalAmount = totalAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OriginalEndDate = request.EndDate,
            MaxExtensionMonths = request.MaxExtensionMonths,
            SideARepresentative = request.SideARepresentative ?? sideA,
            EcontractUrl = request.EcontractUrl,
            Status = ContractStatus.PendingSignature,
            CreatedBy = createdBy
        };
        await _contracts.AddAsync(contract);
        await _contracts.SaveChangesAsync();

        // Gắn các deliverable của project CHƯA thuộc hợp đồng nào vào hợp đồng này
        // (contract ký một tập con sản phẩm — mặc định lấy hết phần chưa ký).
        var unassigned = await _proposals.Deliverables
            .Where(d => d.ProjectId == project.Id && d.ContractId == null)
            .OrderBy(d => d.Sequence)
            .ToListAsync();
        foreach (var d in unassigned)
        {
            d.ContractId = contract.Id;
            d.AcceptanceStatus ??= AcceptanceStatus.Pending;
        }
        await _proposals.SaveChangesAsync();

        return await GetByIdAsync(contract.Id);
    }

    public async Task<ContractDetailResponse> SignAsync(Guid contractId, Guid signedBy)
    {
        var contract = await QueryWithProject()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (contract.Status != ContractStatus.PendingSignature)
            throw new InvalidOperationException(
                $"Cannot sign contract: current status is '{contract.Status}'.");

        contract.Status = ContractStatus.Active;
        contract.SignedAt = _clock.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;

        // Ký hợp đồng → project sang giai đoạn thực hiện.
        contract.Project.Status = ProjectStatus.InProgress;
        contract.Project.UpdatedAt = DateTime.UtcNow;
        await _contracts.SaveChangesAsync();

        return MapDetail(contract);
    }

    private static ContractListResponse MapList(Contract c) => new()
    {
        Id = c.Id,
        ContractNumber = c.ContractNumber,
        ProjectId = c.ProjectId,
        ProposalId = c.Project?.Proposals.FirstOrDefault()?.Id ?? Guid.Empty,
        ProposalCode = c.Project?.ProjectCode,
        ProposalTitle = c.Project?.TitleVi,
        PiName = c.Project?.PiUser?.FullName,
        Status = c.Status,
        TotalAmount = c.TotalAmount,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        OriginalEndDate = c.OriginalEndDate,
        MaxExtensionMonths = c.MaxExtensionMonths,
        SignedAt = c.SignedAt,
        CreatedAt = c.CreatedAt
    };

    private static ContractDetailResponse MapDetail(Contract c) => new()
    {
        Id = c.Id,
        ContractNumber = c.ContractNumber,
        ProjectId = c.ProjectId,
        ProposalId = c.Project?.Proposals.FirstOrDefault()?.Id ?? Guid.Empty,
        ProposalCode = c.Project?.ProjectCode,
        ProposalTitle = c.Project?.TitleVi,
        FundingMethod = c.Project?.Proposals.FirstOrDefault()?.FundingMethod,
        ScopeTitle = c.ScopeTitle,
        Status = c.Status,
        TotalAmount = c.TotalAmount,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        OriginalEndDate = c.OriginalEndDate,
        MaxExtensionMonths = c.MaxExtensionMonths,
        SideARepresentative = c.SideARepresentative,
        EcontractUrl = c.EcontractUrl,
        SignedAt = c.SignedAt,
        TerminatedAt = c.TerminatedAt,
        TerminatedReason = c.TerminatedReason,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
