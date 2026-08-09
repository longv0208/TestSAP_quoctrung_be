using FURPMS.Application.Constants;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class ResearchOrderService : IResearchOrderService
{
    private const string OrderOpen = "OPEN";
    private const string OrderMatched = "MATCHED";

    private readonly ICycleRepository _cycles;
    private readonly IProposalRepository _proposals;

    public ResearchOrderService(ICycleRepository cycles, IProposalRepository proposals)
    {
        _cycles = cycles;
        _proposals = proposals;
    }

    public async Task<int> MatchWinnerAsync(int orderId, Guid winnerProposalId)
    {
        var order = await _cycles.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new KeyNotFoundException("Research order not found.");
        if (order.Status != OrderOpen)
            throw new InvalidOperationException("Chỉ ghép được đề tài vào danh mục đặt hàng đang mở.");

        var winner = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == winnerProposalId)
            ?? throw new KeyNotFoundException("Proposal not found.");
        if (winner.Project.OrderId != orderId)
            throw new ArgumentException("Đề cương này không đăng ký cho đề tài đặt hàng này.");

        order.MatchedProjectId = winner.ProjectId;
        order.Status = OrderMatched;
        winner.Status = ProposalStatus.Approved;
        winner.UpdatedAt = DateTime.UtcNow;
        winner.Project.Status = ProjectStatus.Approved;
        winner.Project.UpdatedAt = DateTime.UtcNow;

        // Auto-loại các đề cương cạnh tranh còn lại của cùng đề tài đặt hàng (1 winner).
        var losers = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project)
            .Where(p => p.Project.OrderId == orderId && p.Id != winner.Id && p.Status != ProposalStatus.Rejected)
            .ToListAsync();
        foreach (var l in losers)
        {
            l.Status = ProposalStatus.Rejected;
            l.RejectionReason = "Không được chọn cho đề tài đặt hàng (đã chọn ứng viên khác).";
            l.UpdatedAt = DateTime.UtcNow;
            l.Project.Status = ProjectStatus.Cancelled;
            l.Project.UpdatedAt = DateTime.UtcNow;
        }

        await _cycles.SaveChangesAsync();
        return losers.Count;
    }
}
