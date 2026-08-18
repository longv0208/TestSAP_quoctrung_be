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
    private readonly ISystemSettingService _settings;

    public ProposalsController(
        IProposalService proposals,
        IProposalExtractionService extraction,
        ISystemSettingService settings)
    {
        _proposals = proposals;
        _extraction = extraction;
        _settings = settings;
    }

    // POST /api/proposals/extract — Đường B: upload Word/PDF → AI trích xuất field để prefill form.
    [HttpPost("extract")]
    public async Task<IActionResult> Extract(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        // Endpoint trích xuất đọc toàn bộ file vào bộ nhớ rồi mới gửi Gemini. Chặn trước khi mở
        // stream để một file quá lớn không làm tốn RAM/quota; dùng đúng cấu hình upload Admin đang
        // quản lý, không dựng thêm một con số riêng cho AI.
        var policy = await _settings.GetUploadPolicyAsync();
        var maxBytes = (long)policy.MaxFileSizeMb * 1024 * 1024;
        if (file.Length > maxBytes)
            throw new ArgumentException(
                $"File quá lớn ({file.Length / 1024d / 1024d:0.#} MB). Tối đa {policy.MaxFileSizeMb} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!policy.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Định dạng '{ext}' không được phép. Chỉ nhận: {string.Join(", ", policy.AllowedExtensions)}.");

        // Gemini đọc PDF trực tiếp; DOCX được bóc văn bản bằng OpenXml. Định dạng DOC cũ không có
        // bộ đọc an toàn trong ứng dụng, nên không nhận rồi để người dùng đợi AI mới báo lỗi.
        if (ext is not ".pdf" and not ".docx")
            throw new ArgumentException(
                $"AI chưa đọc được định dạng '{ext}'. Hãy lưu tài liệu thành PDF hoặc DOCX rồi thử lại.");

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
        var (callerId, roles) = GetCaller();
        var result = await _proposals.GetProposalByIdAsync(id, callerId, roles);
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
        // "Đề cương của tôi" LUÔN chỉ của người gọi — kể cả Admin/Staff (đa vai đang làm PI).
        var result = await _proposals.GetProposalsAsync(new ProposalQueryParams(), callerId, roles, ownOnly: true);
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
