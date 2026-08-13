using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ContractListResponse>>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] bool mine = false)
    {
        var (userId, roles) = GetCaller();
        // mine=true → "HĐ của tôi" LUÔN chỉ của người gọi (kể cả tài khoản đa vai đang "làm PI").
        // Các trang PI (báo cáo tiến độ/sản phẩm/tổng kết) dùng mine=true để không lôi HĐ người khác.
        Guid? piFilter = mine
            ? userId
            : ((roles.Contains("Admin") || roles.Contains("Staff")) ? null : userId);
        var result = await _contracts.GetListAsync(piFilter);
        return Ok(ApiResponse<IEnumerable<ContractListResponse>>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<ContractDetailResponse>), StatusCodes.Status200OK)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        await AuthorizeContractAsync(id);
        var result = await _contracts.GetByIdAsync(id);
        return Ok(ApiResponse<ContractDetailResponse>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<ContractDetailResponse>), StatusCodes.Status200OK)]
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        var (userId, _) = GetCaller();
        var result = await _contracts.CreateAsync(request, userId!.Value);
        return Ok(ApiResponse<ContractDetailResponse>.Ok(result));
    }

    // Staff gõ sai số HĐ / ngày lúc tạo thì phải sửa được — trước đây chỉ có GET/POST/sign nên
    // nhập nhầm là kẹt vĩnh viễn.
    [ProducesResponseType(typeof(ApiResponse<ContractDetailResponse>), StatusCodes.Status200OK)]
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContractRequest request)
    {
        var result = await _contracts.UpdateAsync(id, request);
        return Ok(ApiResponse<ContractDetailResponse>.Ok(result, "Đã cập nhật hợp đồng."));
    }

    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _contracts.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Đã xoá hợp đồng."));
    }

    [ProducesResponseType(typeof(ApiResponse<ContractDetailResponse>), StatusCodes.Status200OK)]
    /// <summary>
    /// <b>Ghi nhận đã ký</b> — hệ thống không ký thay ai (QĐ543 BM05 Điều 7.2: ký điện tử ở phần
    /// mềm ngoài). Bắt buộc đã tải bản đã ký lên trước, ngược lại trả <b>409</b>.
    /// </summary>
    /// <param name="signedOn">Ngày ký ghi trên giấy (yyyy-MM-dd). Bỏ trống thì lấy ngày hôm nay.</param>
    [HttpPost("{id:guid}/sign")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Sign(Guid id, [FromQuery] DateOnly? signedOn)
    {
        var (userId, _) = GetCaller();
        var result = await _contracts.SignAsync(id, userId!.Value, signedOn);
        return Ok(ApiResponse<ContractDetailResponse>.Ok(result, "Đã ghi nhận hợp đồng đã ký."));
    }

    // ── Disbursements ─────────────────────────────────────────────────────────

    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DisbursementResponse>>), StatusCodes.Status200OK)]
    [HttpGet("{contractId:guid}/disbursements")]
    public async Task<IActionResult> GetDisbursements(Guid contractId)
    {
        await AuthorizeContractAsync(contractId);
        var result = await _disbursements.GetByContractAsync(contractId);
        return Ok(ApiResponse<IEnumerable<DisbursementResponse>>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DisbursementResponse>>), StatusCodes.Status200OK)]
    [HttpPost("{contractId:guid}/disbursements/generate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GenerateDisbursements(Guid contractId)
    {
        var result = await _disbursements.GenerateAsync(contractId);
        return Ok(ApiResponse<IEnumerable<DisbursementResponse>>.Ok(result));
    }

    // ── Deliverables ──────────────────────────────────────────────────────────

    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DeliverableResponse>>), StatusCodes.Status200OK)]
    [HttpGet("{contractId:guid}/deliverables")]
    public async Task<IActionResult> GetDeliverables(Guid contractId)
    {
        await AuthorizeContractAsync(contractId);
        var result = await _deliverables.GetByContractAsync(contractId);
        return Ok(ApiResponse<IEnumerable<DeliverableResponse>>.Ok(result));
    }

    // Staff thêm 1 sản phẩm phải nộp cho hợp đồng (đề cương không có trường sản phẩm cấu trúc → nhập tay).
    [ProducesResponseType(typeof(ApiResponse<DeliverableResponse>), StatusCodes.Status200OK)]
    [HttpPost("{contractId:guid}/deliverables")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateDeliverable(Guid contractId, [FromBody] CreateDeliverableRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _deliverables.CreateAsync(contractId, request, userId);
        return Ok(ApiResponse<DeliverableResponse>.Ok(result, "Đã thêm sản phẩm."));
    }

    // ── Amendments ────────────────────────────────────────────────────────────

    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AmendmentListResponse>>), StatusCodes.Status200OK)]
    [HttpGet("{contractId:guid}/amendments")]
    public async Task<IActionResult> GetAmendments(Guid contractId)
    {
        await AuthorizeContractAsync(contractId);
        var result = await _amendments.GetByContractAsync(contractId);
        return Ok(ApiResponse<IEnumerable<AmendmentListResponse>>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<AmendmentDetailResponse>), StatusCodes.Status200OK)]
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
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (piId != userId)
            throw new UnauthorizedAccessException("Chỉ chủ nhiệm đề tài mới xem được hợp đồng này.");
    }
}
