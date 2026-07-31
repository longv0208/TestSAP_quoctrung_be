using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

// Kho tài liệu toàn cục — Admin/Staff xem tất cả file đính kèm kèm context đề tài/PI.
[ApiController]
[Route("api/documents")]
[Authorize(Roles = "Admin,Staff")]
public class DocumentsController : ControllerBase
{
    private readonly IProposalDocumentService _docs;

    public DocumentsController(IProposalDocumentService docs)
    {
        _docs = docs;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _docs.ListAllAsync();
        return Ok(ApiResponse<IEnumerable<ProposalDocumentDto>>.Ok(result));
    }
}
