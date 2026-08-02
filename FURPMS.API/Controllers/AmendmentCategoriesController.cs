using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

/// <summary>
/// Danh mục loại điều chỉnh hợp đồng (gia hạn, đổi kinh phí, đổi nhân sự…).
/// FE cần list này để điền `categoryId` khi tạo yêu cầu điều chỉnh (§9 Điều chỉnh).
/// Chỉ đọc — dữ liệu do seeder tạo theo QĐ 543.
/// </summary>
[ApiController]
[Route("api/amendment-categories")]
[Authorize]
public class AmendmentCategoriesController : ControllerBase
{
    private readonly IMasterDataRepository _repo;

    public AmendmentCategoriesController(IMasterDataRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly)
    {
        var q = _repo.AmendmentCategories.AsQueryable();
        if (activeOnly == true) q = q.Where(c => c.IsActive);
        var list = await q.OrderBy(c => c.Name).ToListAsync();
        return Ok(ApiResponse<List<AmendmentCategory>>.Ok(list));
    }
}
