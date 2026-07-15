namespace FURPMS.Application.Interfaces.Services;

// Applied (đặt hàng): chọn 1 winner cho đề tài đặt hàng → auto-loại các đề cương cạnh tranh.
public interface IResearchOrderService
{
    // Trả về số đề cương cạnh tranh bị loại.
    Task<int> MatchWinnerAsync(int orderId, Guid winnerProposalId);
}
