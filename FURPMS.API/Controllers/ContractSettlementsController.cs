using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Settlements;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
public class ContractSettlementsController : ControllerBase
{
    private readonly IContractSettlementService _service;

    public ContractSettlementsController(IContractSettlementService service) => _service = service;

    [ProducesResponseType(typeof(ApiResponse<SettlementDto?>), StatusCodes.Status200OK)]
    [HttpGet("api/contracts/{contractId:guid}/settlement")]
    public async Task<IActionResult> Get(Guid contractId)
    {
        var dto = await _service.GetByContractAsync(contractId);
        return Ok(ApiResponse<SettlementDto?>.Ok(dto));
    }

    [ProducesResponseType(typeof(ApiResponse<SettlementDto>), StatusCodes.Status200OK)]
    [HttpPost("api/contracts/{contractId:guid}/settlement")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create(Guid contractId, [FromBody] CreateSettlementRequest request)
    {
        var dto = await _service.CreateAsync(contractId, request);
        return Ok(ApiResponse<SettlementDto>.Ok(dto));
    }

    [ProducesResponseType(typeof(ApiResponse<SettlementDto>), StatusCodes.Status200OK)]
    [HttpPost("api/settlements/{id:int}/sign")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Sign(int id, [FromBody] SignSettlementRequest request)
    {
        var dto = await _service.SignAsync(id, request);
        return Ok(ApiResponse<SettlementDto>.Ok(dto));
    }

    [ProducesResponseType(typeof(ApiResponse<SettlementDto>), StatusCodes.Status200OK)]
    [HttpPost("api/settlements/{id:int}/accounting-cleared")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> MarkAccountingCleared(int id, [FromBody] MarkClearedRequest request)
    {
        var dto = await _service.MarkAccountingClearedAsync(id, request.ClearedDate);
        return Ok(ApiResponse<SettlementDto>.Ok(dto));
    }

    [ProducesResponseType(typeof(ApiResponse<SettlementDto>), StatusCodes.Status200OK)]
    [HttpPost("api/settlements/{id:int}/assets-cleared")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> MarkAssetsCleared(int id, [FromBody] MarkClearedRequest request)
    {
        var dto = await _service.MarkAssetsClearedAsync(id, request.ClearedDate);
        return Ok(ApiResponse<SettlementDto>.Ok(dto));
    }
}
