using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contracts;
    private readonly IDisbursementService _disbursements;
    private readonly IDeliverableService _deliverables;
    private readonly IAmendmentService _amendments;
    private readonly IContractRepository _contractRepo;

    public ContractsController(
        IContractService contracts,
        IDisbursementService disbursements,
        IDeliverableService deliverables,
        IAmendmentService amendments,
        IContractRepository contractRepo)
    {
        _contracts = contracts;
        _disbursements = disbursements;
        _deliverables = deliverables;
        _amendments = amendments;
        _contractRepo = contractRepo;
    }

    // ── Contracts ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var (userId, roles) = GetCaller();
        Guid? piFilter = (roles.Contains("Admin") || roles.Contains("Staff")) ? null : userId;
        var result = await _contracts.GetListAsync(piFilter);
        return Ok(ApiResponse<IEnumerable<ContractListResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        await AuthorizeContractAsync(id);
        var result = await _contracts.GetByIdAsync(id);
        return Ok(ApiResponse<ContractDetailResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        var (userId, _) = GetCaller();
        var result = await _contracts.CreateAsync(request, userId!.Value);
        return Ok(ApiResponse<ContractDetailResponse>.Ok(result));
    }

    [HttpPost("{id:guid}/sign")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Sign(Guid id)
    {
        var (userId, _) = GetCaller();
        var result = await _contracts.SignAsync(id, userId!.Value);
        return Ok(ApiResponse<ContractDetailResponse>.Ok(result));
    }

    // ── Disbursements ─────────────────────────────────────────────────────────

    [HttpGet("{contractId:guid}/disbursements")]
    public async Task<IActionResult> GetDisbursements(Guid contractId)
    {
        await AuthorizeContractAsync(contractId);
        var result = await _disbursements.GetByContractAsync(contractId);
        return Ok(ApiResponse<IEnumerable<DisbursementResponse>>.Ok(result));
    }

    [HttpPost("{contractId:guid}/disbursements/generate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GenerateDisbursements(Guid contractId)
    {
        var result = await _disbursements.GenerateAsync(contractId);
        return Ok(ApiResponse<IEnumerable<DisbursementResponse>>.Ok(result));
    }

    // ── Deliverables ──────────────────────────────────────────────────────────

    [HttpGet("{contractId:guid}/deliverables")]
    public async Task<IActionResult> GetDeliverables(Guid contractId)
    {
        await AuthorizeContractAsync(contractId);
        var result = await _deliverables.GetByContractAsync(contractId);
        return Ok(ApiResponse<IEnumerable<DeliverableResponse>>.Ok(result));
    }

    // ── Amendments ────────────────────────────────────────────────────────────

    [HttpGet("{contractId:guid}/amendments")]
    public async Task<IActionResult> GetAmendments(Guid contractId)
    {
        await AuthorizeContractAsync(contractId);
        var result = await _amendments.GetByContractAsync(contractId);
        return Ok(ApiResponse<IEnumerable<AmendmentListResponse>>.Ok(result));
    }

    [HttpPost("{contractId:guid}/amendments")]
    public async Task<IActionResult> CreateAmendment(Guid contractId, [FromBody] CreateAmendmentRequest request)
    {
        await AuthorizeContractAsync(contractId);
        var (userId, _) = GetCaller();
        var result = await _amendments.CreateAsync(contractId, request, userId!.Value);
        return Ok(ApiResponse<AmendmentDetailResponse>.Ok(result));
    }

    // ── Auth helper ───────────────────────────────────────────────────────────

    private (Guid? userId, HashSet<string> roles) GetCaller()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = idStr != null ? Guid.Parse(idStr) : (Guid?)null;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        return (userId, roles);
    }

    private async Task AuthorizeContractAsync(Guid contractId)
    {
        var (userId, roles) = GetCaller();
        if (roles.Contains("Admin") || roles.Contains("Staff"))
            return;

        var piId = await _contractRepo.Query()
            .Where(c => c.Id == contractId)
            .Select(c => (Guid?)c.Project.PiUserId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (piId != userId)
            throw new UnauthorizedAccessException("Access denied: not the PI of this contract's proposal.");
    }
}
