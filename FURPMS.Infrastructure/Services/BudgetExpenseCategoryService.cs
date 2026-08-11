using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class BudgetExpenseCategoryService : IBudgetExpenseCategoryService
{
    private readonly IMasterDataRepository _masterData;
    // Kiểm tham chiếu chéo sang bảng nghiệp vụ ⇒ đọc thẳng DbContext.
    private readonly FURPMSDbContext _db;

    public BudgetExpenseCategoryService(IMasterDataRepository masterData, FURPMSDbContext db)
    {
        _masterData = masterData;
        _db = db;
    }

    public async Task<IEnumerable<BudgetExpenseCategoryResponse>> GetAllAsync()
    {
        var list = await _masterData.BudgetExpenseCategories.OrderBy(x => x.Sequence).ToListAsync();
        return list.Select(Map);
    }

    public async Task<BudgetExpenseCategoryResponse> GetByIdAsync(int id)
    {
        var entity = await _masterData.BudgetExpenseCategories.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"BudgetExpenseCategory {id} not found.");
        return Map(entity);
    }

    public async Task<BudgetExpenseCategoryResponse> CreateAsync(UpsertBudgetExpenseCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Phải nhập mã hạng mục.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Phải nhập tên hạng mục.");

        var exists = await _masterData.BudgetExpenseCategories.AnyAsync(x => x.Code == request.Code);
        if (exists)
            throw new InvalidOperationException($"Đã có hạng mục chi mang mã {request.Code}.");

        var entity = new BudgetExpenseCategory
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Sequence = request.Sequence,
            MaxPercentage = request.MaxPercentage,
            IsActive = request.IsActive
        };
        _masterData.Add(entity);
        await _masterData.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<BudgetExpenseCategoryResponse> UpdateAsync(int id, UpsertBudgetExpenseCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Phải nhập tên hạng mục.");

        var entity = await _masterData.BudgetExpenseCategories.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"BudgetExpenseCategory {id} not found.");

        var codeConflict = await _masterData.BudgetExpenseCategories
            .AnyAsync(x => x.Code == request.Code.ToUpperInvariant() && x.Id != id);
        if (codeConflict)
            throw new InvalidOperationException($"Đã có hạng mục chi mang mã {request.Code}.");

        entity.Code = request.Code.ToUpperInvariant();
        entity.Name = request.Name;
        entity.Sequence = request.Sequence;
        entity.MaxPercentage = request.MaxPercentage;
        entity.IsActive = request.IsActive;
        await _masterData.SaveChangesAsync();
        return Map(entity);
    }

    private static BudgetExpenseCategoryResponse Map(BudgetExpenseCategory e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        Name = e.Name,
        Sequence = e.Sequence,
        MaxPercentage = e.MaxPercentage,
        IsActive = e.IsActive
    };

    /// <summary>
    /// Xoá vĩnh viễn chỉ khi KHÔNG ai tham chiếu — giống cách đã làm với loại đề tài.
    /// Còn dùng thì vô hiệu hoá (<c>isActive = false</c>) để dữ liệu cũ không mồ côi.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var entity = await _masterData.BudgetExpenseCategories.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy hạng mục chi {id}.");

        var used = await _db.ProposalBudgetItems.AnyAsync(i => i.CategoryId == id);
        if (used)
            throw new InvalidOperationException(
                $"Hạng mục chi \"{entity.Name}\" đang có trong dự toán của đề tài — " +
                "chỉ có thể vô hiệu hoá, không xoá vĩnh viễn được.");

        _masterData.Remove(entity);
        await _masterData.SaveChangesAsync();
    }
}
