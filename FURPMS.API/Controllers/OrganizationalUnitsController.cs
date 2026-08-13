using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/organizational-units")]
[Authorize]
public class OrganizationalUnitsController : ControllerBase
{
    private readonly IMasterDataRepository _repo;
    // Kiểm tham chiếu chéo nhiều bảng ngoài phạm vi master data ⇒ đọc thẳng DbContext,
    // không nhét thêm cả chục IQueryable vào IMasterDataRepository chỉ để phục vụ một phép đếm.
    private readonly FURPMSDbContext _db;

    public OrganizationalUnitsController(IMasterDataRepository repo, FURPMSDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repo.OrganizationalUnits
            .Where(u => u.IsActive)
            .OrderBy(u => u.SortOrder).ThenBy(u => u.Name)
            .Select(u => new OrgUnitDto
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Name,
                UnitType = u.UnitType,
                ParentId = u.ParentId,
                HeadUserId = u.HeadUserId,
                IsActive = u.IsActive,
                SortOrder = u.SortOrder,
            })
            .ToListAsync();
        return Ok(ApiResponse<List<OrgUnitDto>>.Ok(list));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] OrgUnitRequest request)
    {
        var entity = new OrganizationalUnit
        {
            Code = request.Code,
            Name = request.Name,
            UnitType = request.UnitType,
            ParentId = request.ParentId,
            HeadUserId = request.HeadUserId,
            SortOrder = request.SortOrder,
            IsActive = true,
        };
        _repo.Add(entity);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse<OrganizationalUnit>.Ok(entity));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] OrgUnitRequest request)
    {
        var entity = await _repo.OrganizationalUnits.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn vị.");

        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.UnitType = request.UnitType;
        entity.ParentId = request.ParentId;
        entity.HeadUserId = request.HeadUserId;
        entity.SortOrder = request.SortOrder;
        _repo.Update(entity);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse<OrganizationalUnit>.Ok(entity));
    }

    /// <summary>
    /// Xoá vĩnh viễn chỉ khi KHÔNG ai tham chiếu — đơn vị đang gắn với người dùng, đề tài hay
    /// danh mục đặt hàng mà xoá là làm mồ côi cả cụm dữ liệu đó. Còn dùng thì **vô hiệu hoá**
    /// (`isActive = false`), giống cách đã làm với loại đề tài.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _repo.OrganizationalUnits.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy đơn vị {id}.");

        var blockers = new List<string>();
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.UnitId == id)) blockers.Add("người dùng");
        if (await _db.Projects.IgnoreQueryFilters().AnyAsync(p => p.HostingUnitId == id)) blockers.Add("đề tài");
        if (await _db.ResearchOrders.AnyAsync(o => o.OrderingUnitId == id)) blockers.Add("danh mục đặt hàng");
        if (await _db.OrganizationalUnits.AnyAsync(u => u.ParentId == id)) blockers.Add("đơn vị con");
        if (blockers.Count > 0)
            throw new InvalidOperationException(
                $"Đơn vị \"{entity.Name}\" đang được {string.Join(", ", blockers)} sử dụng — " +
                "chỉ có thể vô hiệu hoá, không xoá vĩnh viễn được.");

        _repo.Remove(entity);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Đã xoá vĩnh viễn đơn vị."));
    }
}

public class OrgUnitDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string UnitType { get; set; } = null!;
    public int? ParentId { get; set; }
    public Guid? HeadUserId { get; set; }
    public bool IsActive { get; set; }
    public int? SortOrder { get; set; }
}

public class OrgUnitRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string UnitType { get; set; } = null!;
    public int? ParentId { get; set; }
    public Guid? HeadUserId { get; set; }
    public int? SortOrder { get; set; }
}
