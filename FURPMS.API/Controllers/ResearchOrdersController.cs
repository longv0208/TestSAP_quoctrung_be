using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Cycles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/research-orders")]
public class ResearchOrdersController : ControllerBase
{
    private readonly ICycleRepository _cycles;
    private readonly IMasterDataRepository _masterData;
    private readonly IProposalRepository _proposals;
    private readonly IResearchOrderService _orderService;

    public ResearchOrdersController(
        ICycleRepository cycles, IMasterDataRepository masterData,
        IProposalRepository proposals, IResearchOrderService orderService)
    {
        _cycles = cycles;
        _masterData = masterData;
        _proposals = proposals;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? cycleId, [FromQuery] string? status)
    {
        var q = _cycles.Orders.Include(o => o.OrderingUnit).AsQueryable();
        if (cycleId.HasValue) q = q.Where(o => o.CycleId == cycleId.Value);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(o => o.Status == status);

        var orders = await q.OrderByDescending(o => o.CreatedAt).ToListAsync();
        var orderIds = orders.Select(o => o.Id).ToList();

        // Đếm số PI đã đăng ký mỗi đề tài đặt hàng (nhiều PI cùng OrderId = cạnh tranh).
        var counts = await _proposals.Projects.IgnoreQueryFilters()
            .Where(p => orderIds.Contains(p.OrderId) && !p.IsDeleted)
            .GroupBy(p => p.OrderId)
            .Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToListAsync();
        var countMap = counts.ToDictionary(c => c.OrderId, c => c.Count);

        var list = orders.Select(o => new ResearchOrderDto
        {
            Id = o.Id,
            CycleId = o.CycleId,
            OrderingUnitId = o.OrderingUnitId,
            OrderingUnitName = o.OrderingUnit.Name,
            ResearchArea = o.ResearchArea,
            ProblemDescription = o.ProblemDescription,
            ExpectedProducts = o.ExpectedProducts,
            Status = o.Status,
            MatchedProposalId = o.MatchedProjectId,
            RegisteredCount = countMap.GetValueOrDefault(o.Id),
            CreatedBy = o.CreatedBy,
            CreatedAt = o.CreatedAt,
        }).ToList();
        return Ok(ApiResponse<List<ResearchOrderDto>>.Ok(list));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var o = await _cycles.Orders.Include(o => o.OrderingUnit)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException("Research order not found.");
        return Ok(ApiResponse<ResearchOrderDto>.Ok(new ResearchOrderDto
        {
            Id = o.Id, CycleId = o.CycleId, OrderingUnitId = o.OrderingUnitId,
            OrderingUnitName = o.OrderingUnit.Name, ResearchArea = o.ResearchArea,
            ProblemDescription = o.ProblemDescription, ExpectedProducts = o.ExpectedProducts,
            Status = o.Status, MatchedProposalId = o.MatchedProjectId,
            CreatedBy = o.CreatedBy, CreatedAt = o.CreatedAt,
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResearchOrderRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var unitExists = await _masterData.OrganizationalUnits
            .AnyAsync(u => u.Id == request.OrderingUnitId);
        if (!unitExists) throw new KeyNotFoundException("Organizational unit not found.");

        var order = new ResearchOrder
        {
            CycleId = request.CycleId,
            OrderingUnitId = request.OrderingUnitId,
            ResearchArea = request.ResearchArea,
            ProblemDescription = request.ProblemDescription,
            ExpectedProducts = request.ExpectedProducts,
            Status = "OPEN",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
        };
        await _cycles.AddOrderAsync(order);
        await _cycles.SaveChangesAsync();
        return Ok(ApiResponse<ResearchOrderDto>.Ok(new ResearchOrderDto
        {
            Id = order.Id, CycleId = order.CycleId, OrderingUnitId = order.OrderingUnitId,
            ResearchArea = order.ResearchArea, ProblemDescription = order.ProblemDescription,
            ExpectedProducts = order.ExpectedProducts, Status = order.Status,
            CreatedBy = order.CreatedBy, CreatedAt = order.CreatedAt,
        }));
    }

    // Chọn winner cho đề tài đặt hàng: 1 đề cương được duyệt → các đề cương cạnh tranh còn lại bị loại.
    [HttpPost("{id:int}/match")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> MatchProposal(int id, [FromBody] MatchProposalRequest request)
    {
        var rejected = await _orderService.MatchWinnerAsync(id, request.ProposalId);
        return Ok(ApiResponse.Ok($"Đã chọn winner; loại {rejected} đề cương cạnh tranh."));
    }
}

public class ResearchOrderDto
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public int OrderingUnitId { get; set; }
    public string? OrderingUnitName { get; set; }
    public string ResearchArea { get; set; } = null!;
    public string ProblemDescription { get; set; } = null!;
    public string? ExpectedProducts { get; set; }
    public string Status { get; set; } = null!;
    public Guid? MatchedProposalId { get; set; }
    public int RegisteredCount { get; set; }   // số PI đã đăng ký (cạnh tranh)
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateResearchOrderRequest
{
    public int CycleId { get; set; }
    public int OrderingUnitId { get; set; }
    public string ResearchArea { get; set; } = null!;
    public string ProblemDescription { get; set; } = null!;
    public string? ExpectedProducts { get; set; }
}

public class MatchProposalRequest
{
    public Guid ProposalId { get; set; }
}
