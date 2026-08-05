using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Progress;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class AmendmentService : IAmendmentService
{
    private readonly IContractRepository _contracts;
    private readonly IMasterDataRepository _masterData;
    private readonly IClock _clock;

    public AmendmentService(IContractRepository contracts, IMasterDataRepository masterData,
        IClock clock)
    {
        _contracts = contracts;
        _masterData = masterData;
        _clock = clock;
    }

    public async Task<IEnumerable<AmendmentListResponse>> GetByContractAsync(Guid contractId)
    {
        _ = await _contracts.Query().FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        var items = await _contracts.Amendments
            .Include(a => a.Category)
            .Where(a => a.ContractId == contractId)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync();

        return items.Select(MapList);
    }

    public async Task<AmendmentDetailResponse> GetByIdAsync(Guid amendmentId)
    {
        var a = await _contracts.Amendments
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == amendmentId)
            ?? throw new KeyNotFoundException($"Amendment {amendmentId} not found.");
        return MapDetail(a);
    }

    public async Task<AmendmentDetailResponse> CreateAsync(
        Guid contractId, CreateAmendmentRequest request, Guid requestedBy)
    {
        _ = await _contracts.Query().FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        _ = await _masterData.AmendmentCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId)
            ?? throw new KeyNotFoundException($"Amendment category {request.CategoryId} not found.");

        var amendment = new AmendmentRequest
        {
            ContractId = contractId,
            CategoryId = request.CategoryId,
            ChangeDescription = request.ChangeDescription,
            Justification = request.Justification,
            ChangePercentage = request.ChangePercentage,
            OldValue = request.OldValue,
            NewValue = request.NewValue,
            RequiresRectorApproval = request.RequiresRectorApproval,
            RequestedBy = requestedBy,
            Status = AmendmentStatus.Pending
        };
        await _contracts.AddAmendmentAsync(amendment);
        await _contracts.SaveChangesAsync();

        return await GetByIdAsync(amendment.Id);
    }

    public async Task<AmendmentDetailResponse> ApproveAsync(
        Guid amendmentId, ReviewAmendmentRequest request, Guid reviewedBy)
    {
        var amendment = await _contracts.Amendments
            .Include(a => a.Category)
            .Include(a => a.Contract)
            .FirstOrDefaultAsync(a => a.Id == amendmentId)
            ?? throw new KeyNotFoundException($"Amendment {amendmentId} not found.");

        if (amendment.Status != AmendmentStatus.Pending)
            throw new InvalidOperationException($"Amendment is already '{amendment.Status}'.");

        amendment.Status = AmendmentStatus.Approved;
        amendment.ReviewedBy = reviewedBy;
        amendment.ReviewedAt = _clock.UtcNow;
        amendment.ReviewerComments = request.ReviewerComments;

        if (amendment.Category?.Code == "EXTENSION")
            await ApplyExtensionIfNeededAsync(amendment);

        await _contracts.SaveChangesAsync();
        return MapDetail(amendment);
    }

    public async Task<AmendmentDetailResponse> RejectAsync(
        Guid amendmentId, ReviewAmendmentRequest request, Guid reviewedBy)
    {
        var amendment = await _contracts.Amendments
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == amendmentId)
            ?? throw new KeyNotFoundException($"Amendment {amendmentId} not found.");

        if (amendment.Status != AmendmentStatus.Pending)
            throw new InvalidOperationException($"Amendment is already '{amendment.Status}'.");

        amendment.Status = AmendmentStatus.Rejected;
        amendment.ReviewedBy = reviewedBy;
        amendment.ReviewedAt = _clock.UtcNow;
        amendment.ReviewerComments = request.ReviewerComments;

        await _contracts.SaveChangesAsync();
        return MapDetail(amendment);
    }

    private async Task ApplyExtensionIfNeededAsync(AmendmentRequest amendment)
    {
        // Trước đây chỗ này `return` LẶNG LẼ khi NewValue không phải số nguyên dương
        // (PI gõ "3 tháng" thay vì "3") ⇒ Staff bấm Duyệt, hệ thống báo thành công,
        // nhưng hạn hợp đồng KHÔNG hề đổi và không ai biết. Phải báo lỗi rõ.
        if (!int.TryParse(amendment.NewValue, out int extensionMonths) || extensionMonths <= 0)
            throw new ArgumentException(
                "Yêu cầu gia hạn phải ghi SỐ THÁNG ở ô \"Giá trị đề nghị\" (ví dụ: 3). " +
                $"Hiện đang là \"{amendment.NewValue}\" — không áp dụng được.");

        var contract = amendment.Contract
            ?? await _contracts.Query().FirstOrDefaultAsync(c => c.Id == amendment.ContractId);
        if (contract == null) return;

        var previousExtensions = await _contracts.Amendments
            .Where(a => a.ContractId == contract.Id
                && a.Id != amendment.Id
                && a.Status == AmendmentStatus.Approved
                && a.NewValue != null)
            .ToListAsync();

        int alreadyGranted = previousExtensions
            .Where(a => int.TryParse(a.NewValue, out _))
            .Sum(a => int.Parse(a.NewValue!));

        int totalExtension = alreadyGranted + extensionMonths;
        if (totalExtension > contract.MaxExtensionMonths)
            throw new ArgumentException(
                $"Total extension ({totalExtension} months) exceeds maximum allowed ({contract.MaxExtensionMonths} months).");

        contract.EndDate = contract.OriginalEndDate.AddMonths(totalExtension);
        contract.UpdatedAt = DateTime.UtcNow;
    }

    private static AmendmentListResponse MapList(AmendmentRequest a) => new()
    {
        Id = a.Id,
        ContractId = a.ContractId,
        CategoryId = a.CategoryId,
        CategoryName = a.Category?.Name,
        ChangeDescription = a.ChangeDescription,
        Justification = a.Justification,
        Status = a.Status,
        RequestedAt = a.RequestedAt,
        OldValue = a.OldValue,
        NewValue = a.NewValue
    };

    private static AmendmentDetailResponse MapDetail(AmendmentRequest a) => new()
    {
        Id = a.Id,
        ContractId = a.ContractId,
        CategoryId = a.CategoryId,
        CategoryName = a.Category?.Name,
        ChangeDescription = a.ChangeDescription,
        Justification = a.Justification,
        Status = a.Status,
        RequestedAt = a.RequestedAt,
        OldValue = a.OldValue,
        NewValue = a.NewValue,
        ChangePercentage = a.ChangePercentage,
        RequiresRectorApproval = a.RequiresRectorApproval,
        ReviewerComments = a.ReviewerComments,
        ReviewedAt = a.ReviewedAt
    };
}
