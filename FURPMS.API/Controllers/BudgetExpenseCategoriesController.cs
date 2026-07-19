using FURPMS.Application.Common;
using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/budget-expense-categories")]
[Authorize]
public class BudgetExpenseCategoriesController : ControllerBase
{
    private readonly IBudgetExpenseCategoryService _service;

    public BudgetExpenseCategoriesController(IBudgetExpenseCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<BudgetExpenseCategoryResponse>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<BudgetExpenseCategoryResponse>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<BudgetExpenseCategoryResponse>>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<BudgetExpenseCategoryResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BudgetExpenseCategoryResponse>>> Create(
        [FromBody] UpsertBudgetExpenseCategoryRequest request)
    {
        var result = await _service.CreateAsync(request);
        return Ok(ApiResponse<BudgetExpenseCategoryResponse>.Ok(result, "BudgetExpenseCategory created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<BudgetExpenseCategoryResponse>>> Update(
        int id,
        [FromBody] UpsertBudgetExpenseCategoryRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(ApiResponse<BudgetExpenseCategoryResponse>.Ok(result, "BudgetExpenseCategory updated."));
    }
}
