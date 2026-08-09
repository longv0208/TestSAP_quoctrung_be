using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Settlements;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class ContractSettlementService : IContractSettlementService
{
    private readonly IContractRepository _contracts;
    private readonly IUserRepository _users;
    private readonly IClock _clock;

    public ContractSettlementService(IContractRepository contracts, IUserRepository users,
        IClock clock)
    {
        _contracts = contracts;
        _users = users;
        _clock = clock;
    }

    public async Task<SettlementDto?> GetByContractAsync(Guid contractId)
    {
        var settlement = await _contracts.Settlements
            .Include(s => s.SideASignee)
            .FirstOrDefaultAsync(s => s.ContractId == contractId);

        return settlement == null ? null : ToDto(settlement);
    }

    public async Task<SettlementDto> CreateAsync(Guid contractId, CreateSettlementRequest request)
    {
        var contract = await _contracts.Query()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Contract not found.");

        var exists = await _contracts.Settlements.AnyAsync(s => s.ContractId == contractId);
        if (exists)
            throw new InvalidOperationException("A settlement already exists for this contract.");

        if (request.TotalContractedAmount < 0 || request.TotalDisbursedAmount < 0 || request.TotalReturnedAmount < 0)
            throw new ArgumentException("Amounts must be non-negative.");

        // Quyết toán là bước ĐÓNG hợp đồng, nên phải đi sau khi mọi mốc giải ngân đã xong —
        // trong đó đợt cuối chỉ mở sau khi nghiệm thu Đạt (BM05 Điều 4.2, xem DisbursementService).
        // Không chặn ở đây thì hợp đồng đóng được trong khi vẫn còn đợt treo, và cái treo đó không
        // còn đường nào để chi nữa.
        var pending = await _contracts.Disbursements
            .Where(x => x.ContractId == contractId && x.Status != DisbursementStatus.Disbursed)
            .OrderBy(x => x.RoundNumber)
            .Select(x => x.RoundNumber)
            .ToListAsync();
        if (pending.Count > 0)
            throw new InvalidOperationException(
                $"Còn {pending.Count} đợt giải ngân chưa đánh dấu đã chi (đợt {string.Join(", ", pending)}) " +
                "— chưa thể lập quyết toán. Đợt cuối chỉ mở sau khi hội đồng nghiệm thu kết luận Đạt.");

        var settlement = new ContractSettlement
        {
            ContractId = contractId,
            TotalContractedAmount = request.TotalContractedAmount,
            TotalDisbursedAmount = request.TotalDisbursedAmount,
            TotalReturnedAmount = request.TotalReturnedAmount,
            ProductsSubmittedSummary = request.ProductsSubmittedSummary,
            SettlementDeadline = request.SettlementDeadline,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        await _contracts.AddSettlementAsync(settlement);
        await _contracts.SaveChangesAsync();
        return ToDto(settlement);
    }

    public async Task<SettlementDto> SignAsync(int settlementId, SignSettlementRequest request)
    {
        var settlement = await _contracts.Settlements
            .Include(s => s.SideASignee)
            .FirstOrDefaultAsync(s => s.Id == settlementId)
            ?? throw new KeyNotFoundException("Settlement not found.");

        if (settlement.SettlementSignedAt.HasValue)
            throw new InvalidOperationException("Settlement is already signed.");

        var signee = await _users.Query().FirstOrDefaultAsync(u => u.Id == request.SideASigneeId)
            ?? throw new KeyNotFoundException("Signee user not found.");

        settlement.SideASigneeId = request.SideASigneeId;
        settlement.SettlementSignedAt = _clock.UtcNow;
        settlement.SideASignee = signee;

        await _contracts.SaveChangesAsync();
        return ToDto(settlement);
    }

    public async Task<SettlementDto> MarkAccountingClearedAsync(int settlementId, DateOnly? clearedDate)
    {
        var settlement = await _contracts.Settlements
            .FirstOrDefaultAsync(s => s.Id == settlementId)
            ?? throw new KeyNotFoundException("Settlement not found.");

        settlement.AccountingClearedAt = clearedDate ?? DateOnly.FromDateTime(_clock.UtcNow);
        await _contracts.SaveChangesAsync();
        return ToDto(settlement);
    }

    public async Task<SettlementDto> MarkAssetsClearedAsync(int settlementId, DateOnly? clearedDate)
    {
        var settlement = await _contracts.Settlements
            .FirstOrDefaultAsync(s => s.Id == settlementId)
            ?? throw new KeyNotFoundException("Settlement not found.");

        settlement.AssetsClearedAt = clearedDate ?? DateOnly.FromDateTime(_clock.UtcNow);
        await _contracts.SaveChangesAsync();
        return ToDto(settlement);
    }

    private static SettlementDto ToDto(ContractSettlement s) => new()
    {
        Id = s.Id,
        ContractId = s.ContractId,
        TotalContractedAmount = s.TotalContractedAmount,
        TotalDisbursedAmount = s.TotalDisbursedAmount,
        TotalReturnedAmount = s.TotalReturnedAmount,
        ProductsSubmittedSummary = s.ProductsSubmittedSummary,
        AccountingClearedAt = s.AccountingClearedAt,
        AssetsClearedAt = s.AssetsClearedAt,
        SettlementSignedAt = s.SettlementSignedAt,
        SideASigneeId = s.SideASigneeId,
        SideASigneeName = s.SideASignee?.FullName,
        SettlementDeadline = s.SettlementDeadline,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt,
    };
}
