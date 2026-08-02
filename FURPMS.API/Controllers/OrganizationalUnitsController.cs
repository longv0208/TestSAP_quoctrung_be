using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Users;
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

    public OrganizationalUnitsController(IMasterDataRepository repo) => _repo = repo;

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
            ?? throw new KeyNotFoundException("Organizational unit not found.");

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
