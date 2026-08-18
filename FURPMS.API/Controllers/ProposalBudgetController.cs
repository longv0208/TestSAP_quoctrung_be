using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Budget;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/proposals/{proposalId:guid}/budget")]
[Authorize]
public class ProposalBudgetController : ControllerBase
{
    private readonly IProposalBudgetService _budgetService;
    private readonly IBudgetPolicyService _budgetPolicy;

    public ProposalBudgetController(
        IProposalBudgetService budgetService,
        IBudgetPolicyService budgetPolicy)
    {
        _budgetService = budgetService;
        _budgetPolicy = budgetPolicy;
    }

    /// <summary>
    /// Trần kinh phí đang áp cho đề cương này (QĐ543 Điều 14) — để form hiện sẵn giới hạn thay vì
    /// bắt chủ nhiệm điền hết rồi mới ăn lỗi.
    /// </summary>
    [HttpGet("cap")]
    public async Task<ActionResult<ApiResponse<BudgetCapInfo>>> GetCap(Guid proposalId)
    {
        var result = await _budgetPolicy.GetCapAsync(proposalId);
        return Ok(ApiResponse<BudgetCapInfo>.Ok(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<BudgetResponse>>> GetBudget(Guid proposalId)
    {
        var result = await _budgetService.GetBudgetAsync(proposalId);
        return Ok(ApiResponse<BudgetResponse>.Ok(result));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<BudgetResponse>>> UpdateBudget(
        Guid proposalId,
        [FromBody] UpdateBudgetRequest request)
    {
        var result = await _budgetService.UpdateBudgetAsync(proposalId, request);
        return Ok(ApiResponse<BudgetResponse>.Ok(result, "Budget updated."));
    }

    [HttpGet("labor")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LaborDetailResponse>>>> GetLaborDetails(Guid proposalId)
    {
        var result = await _budgetService.GetLaborDetailsAsync(proposalId);
        return Ok(ApiResponse<IEnumerable<LaborDetailResponse>>.Ok(result));
    }

    [HttpPut("labor/{detailId:int}")]
    public async Task<ActionResult<ApiResponse<LaborDetailResponse>>> UpdateLaborDetail(
        Guid proposalId,
        int detailId,
        [FromBody] UpdateLaborDetailRequest request)
    {
        var result = await _budgetService.UpdateLaborDetailAsync(proposalId, detailId, request);
        return Ok(ApiResponse<LaborDetailResponse>.Ok(result, "Labor detail updated."));
    }
}
