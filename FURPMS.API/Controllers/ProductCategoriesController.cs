using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.MasterData;
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

    public ProductCategoriesController(IMasterDataRepository repo) => _repo = repo;

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
}

public class ProductCategoryRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool? IsActive { get; set; }
}
