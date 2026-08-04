using FURPMS.Application.DTOs.Contract;

namespace FURPMS.Application.Interfaces.Services;

public interface IDisbursementService
{
    Task<IEnumerable<DisbursementResponse>> GetByContractAsync(Guid contractId);
    Task<IEnumerable<DisbursementResponse>> GenerateAsync(Guid contractId);
    Task<DisbursementResponse> ConfirmAsync(int disbursementId, ConfirmDisbursementRequest request, Guid processedBy);

    /// <summary>
    /// Gắn (hoặc gỡ, khi <c>DeliverableId = null</c>) sản phẩm minh chứng cho một đợt giải ngân.
    /// Lúc sinh đợt chỉ PARTIAL mới tự ghép theo thứ tự, WHOLE không có gì —
    /// đây là chỗ để Staff sửa/bổ sung cho đúng thực tế.
    /// </summary>
    Task<DisbursementResponse> LinkDeliverableAsync(int disbursementId, LinkDeliverableRequest request);
}
