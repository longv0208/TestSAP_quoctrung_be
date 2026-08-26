using FURPMS.Application.Common;
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
    private readonly IDecisionLogger _decisions;

    public ContractSettlementService(IContractRepository contracts, IUserRepository users,
        IClock clock,
        IDecisionLogger decisions)
    {
        _decisions = decisions;
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
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (contract.Status == ContractStatus.Terminated)
            throw new InvalidOperationException("Hợp đồng đã chấm dứt — không lập hồ sơ thanh lý theo luồng hoàn thành bình thường.");
        if (contract.Project.Status != ProjectStatus.Completed)
            throw new InvalidOperationException(
                "Đề tài chưa được Hội đồng nghiệm thu kết luận Đạt — chưa thể lập hồ sơ thanh lý BM13.");

        var exists = await _contracts.Settlements.AnyAsync(s => s.ContractId == contractId);
        if (exists)
            throw new InvalidOperationException("Hợp đồng này đã có bản quyết toán.");

        if (request.TotalContractedAmount < 0 || request.TotalDisbursedAmount < 0 || request.TotalReturnedAmount < 0)
            throw new ArgumentException("Các khoản tiền không được âm.");

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
            .Include(s => s.Contract)
            .FirstOrDefaultAsync(s => s.Id == settlementId)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ quyết toán.");

        if (settlement.SettlementSignedAt.HasValue)
            throw new InvalidOperationException("Bản quyết toán đã ký, không sửa được nữa.");

        // BM13 xác nhận đã giao nộp sản phẩm, tiền và tài sản. Ký trước hai xác nhận này làm biên
        // bản tuyên bố hoàn tất trong khi hồ sơ vẫn còn mở; UI cũ còn xếp nút Ký lên trước nên tự
        // khoá luôn hai bước sau.
        if (!settlement.AccountingClearedAt.HasValue || !settlement.AssetsClearedAt.HasValue)
            throw new InvalidOperationException(
                "Phải xác nhận đã quyết toán kinh phí và xử lý tài sản trước khi ký Biên bản thanh lý BM13.");
        if (settlement.Contract.Status == ContractStatus.Terminated)
            throw new InvalidOperationException("Hợp đồng đã chấm dứt — không thể ký thanh lý theo luồng hoàn thành bình thường.");

        var signee = await _users.Query().FirstOrDefaultAsync(u => u.Id == request.SideASigneeId)
            ?? throw new KeyNotFoundException("Không tìm thấy người ký.");

        settlement.SideASigneeId = request.SideASigneeId;
        settlement.SettlementSignedAt = _clock.UtcNow;
        settlement.SideASignee = signee;
        settlement.Contract.Status = ContractStatus.Settled;
        settlement.Contract.UpdatedAt = _clock.UtcNow;

        var hoanTra = settlement.TotalReturnedAmount > 0
            ? $", hoàn trả {settlement.TotalReturnedAmount:N0} đ" : "";
        _decisions.Log(
            settlement.Contract.ProjectId, DecisionTypes.SettlementSigned,
            $"Ký biên bản thanh lý — đã chi {settlement.TotalDisbursedAmount:N0} đ{hoanTra}",
            "ContractSettlement", settlement.Id.ToString(),
            result: "SIGNED", reason: settlement.Notes, documentNo: "BM13",
            decidedBy: request.SideASigneeId, decidedByRole: "Đại diện Bên A");

        await _contracts.SaveChangesAsync();
        return ToDto(settlement);
    }

    /// <summary>
    /// Đánh dấu kế toán đã tất toán — QĐ543 <b>Điều 13.1.e</b> (*"Xác nhận của Ban kế toán về việc
    /// đề tài đã quyết toán kinh phí và đã xử lý tài sản"*).
    /// <para>
    /// <paramref name="clear"/> = <c>false</c> để <b>BỎ đánh dấu</b>: trước đây bấm nhầm là chịu,
    /// không có đường lui, mà đây là mốc đóng hồ sơ tài chính của cả đề tài.
    /// </para>
    /// </summary>
    public async Task<SettlementDto> MarkAccountingClearedAsync(
        int settlementId, DateOnly? clearedDate, bool clear = true)
    {
        var settlement = await _contracts.Settlements
            .FirstOrDefaultAsync(s => s.Id == settlementId)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ quyết toán.");

        AssertUnlocked(settlement);
        settlement.AccountingClearedAt = clear ? clearedDate ?? DateOnly.FromDateTime(_clock.UtcNow) : null;
        await _contracts.SaveChangesAsync();
        return ToDto(settlement);
    }

    /// <summary>Đánh dấu (hoặc bỏ đánh dấu) đã xử lý tài sản — cùng căn cứ Điều 13.1.e.</summary>
    public async Task<SettlementDto> MarkAssetsClearedAsync(
        int settlementId, DateOnly? clearedDate, bool clear = true)
    {
        var settlement = await _contracts.Settlements
            .FirstOrDefaultAsync(s => s.Id == settlementId)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ quyết toán.");

        AssertUnlocked(settlement);
        settlement.AssetsClearedAt = clear ? clearedDate ?? DateOnly.FromDateTime(_clock.UtcNow) : null;
        await _contracts.SaveChangesAsync();
        return ToDto(settlement);
    }

    /// <summary>
    /// Biên bản thanh lý đã ký rồi thì <b>khoá</b> hai mốc trên — Điều 13.2 coi việc ký biên bản
    /// thanh lý là chốt sổ; sửa sau đó là làm lệch với văn bản đã ký.
    /// </summary>
    private static void AssertUnlocked(ContractSettlement settlement)
    {
        if (settlement.SettlementSignedAt != null)
            throw new InvalidOperationException(
                "Biên bản thanh lý đã ký — không đổi được mốc quyết toán nữa. " +
                "Muốn sửa phải huỷ chữ ký biên bản thanh lý trước.");
    }

    private SettlementDto ToDto(ContractSettlement s) => new()
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
        DaysLeft = DeadlineMath.DaysLeft(s.SettlementDeadline, _clock),
        Notes = s.Notes,
        CreatedAt = s.CreatedAt,
    };
}
