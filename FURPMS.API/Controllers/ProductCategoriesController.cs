using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/product-categories")]
[Authorize]
public class ProductCategoriesController : ControllerBase
{
    private readonly IMasterDataRepository _repo;
    // Kiểm tham chiếu chéo nhiều bảng ngoài phạm vi master data ⇒ đọc thẳng DbContext,
    // không nhét thêm cả chục IQueryable vào IMasterDataRepository chỉ để phục vụ một phép đếm.
    private readonly FURPMSDbContext _db;

    public ProductCategoriesController(IMasterDataRepository repo, FURPMSDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly)
    {
        var q = _repo.ProductCategories.AsQueryable();
        if (activeOnly == true) q = q.Where(c => c.IsActive);
        var list = await q.OrderBy(c => c.Name).ToListAsync();
        return Ok(ApiResponse<List<ProductCategory>>.Ok(list));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _repo.ProductCategories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Product category not found.");
        return Ok(ApiResponse<ProductCategory>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] ProductCategoryRequest request)
    {
        var exists = await _repo.ProductCategories.AnyAsync(c => c.Code == request.Code);
        if (exists) throw new InvalidOperationException($"Code '{request.Code}' already exists.");

        var entity = new ProductCategory { Code = request.Code, Name = request.Name, IsActive = true };
        _repo.Add(entity);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse<ProductCategory>.Ok(entity));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductCategoryRequest request)
    {
        var entity = await _repo.ProductCategories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Product category not found.");

        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.IsActive = request.IsActive ?? entity.IsActive;
        _repo.Update(entity);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse<ProductCategory>.Ok(entity));
    }

    /// <summary>Xoá vĩnh viễn chỉ khi chưa sản phẩm nào xếp vào loại này; còn dùng thì vô hiệu hoá.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _repo.ProductCategories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy loại sản phẩm {id}.");

        if (await _db.ProjectDeliverables.AnyAsync(d => d.CategoryId == id))
            throw new InvalidOperationException(
                $"Loại sản phẩm \"{entity.Name}\" đang được sản phẩm của đề tài sử dụng — " +
                "chỉ có thể vô hiệu hoá, không xoá vĩnh viễn được.");

        _repo.Remove(entity);
        await _repo.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Đã xoá vĩnh viễn loại sản phẩm."));
    }
}

public class ProductCategoryRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool? IsActive { get; set; }
}
