using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/proposals")]
[Authorize]
public class ProposalsController : ControllerBase
{
    private readonly IProposalService _proposals;
    private readonly IProposalExtractionService _extraction;

    public ProposalsController(IProposalService proposals, IProposalExtractionService extraction)
    {
        _proposals = proposals;
        _extraction = extraction;
    }

    // POST /api/proposals/extract — Đường B: upload Word/PDF → AI trích xuất field để prefill form.
    [HttpPost("extract")]
    public async Task<IActionResult> Extract(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        await using var stream = file.OpenReadStream();
        var result = await _extraction.ExtractAsync(stream, file.FileName, file.ContentType);
        return Ok(ApiResponse<ExtractedProposalDto>.Ok(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProposalQueryParams queryParams)
    {
        var (callerId, roles) = GetCaller();
        var result = await _proposals.GetProposalsAsync(queryParams, callerId, roles);
        return Ok(ApiResponse<IEnumerable<ProposalSummaryDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _proposals.GetProposalByIdAsync(id);
        return Ok(ApiResponse<ProposalDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProposalRequest request)
    {
        var (callerId, _) = GetCaller();
        var result = await _proposals.CreateProposalAsync(request, callerId);
        return Ok(ApiResponse<ProposalDto>.Ok(result));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var (callerId, roles) = GetCaller();
        var result = await _proposals.GetProposalsAsync(new ProposalQueryParams(), callerId, roles);
        return Ok(ApiResponse<IEnumerable<ProposalSummaryDto>>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateProposalRequest request)
    {
        var (callerId, _) = GetCaller();
        var result = await _proposals.UpdateProposalAsync(id, request, callerId);
        return Ok(ApiResponse<ProposalDto>.Ok(result));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromQuery] bool confirmCv = false)
    {
        var (callerId, _) = GetCaller();
        var result = await _proposals.SubmitProposalAsync(id, callerId, confirmCv);
        return Ok(ApiResponse<ProposalDto>.Ok(result));
    }

    [HttpPatch("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        var (callerId, _) = GetCaller();
        var result = await _proposals.WithdrawProposalAsync(id, callerId);
        return Ok(ApiResponse<ProposalDto>.Ok(result));
    }

    private (Guid callerId, HashSet<string> roles) GetCaller()
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        return (callerId, roles);
    }
}
