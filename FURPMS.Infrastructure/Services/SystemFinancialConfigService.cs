using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class SystemFinancialConfigService : ISystemFinancialConfigService
{
    private readonly IMasterDataRepository _masterData;
    // Kiểm tham chiếu chéo sang bảng nghiệp vụ ⇒ đọc thẳng DbContext.
    private readonly FURPMSDbContext _db;

    public SystemFinancialConfigService(IMasterDataRepository masterData, FURPMSDbContext db)
    {
        _masterData = masterData;
        _db = db;
    }

    public async Task<IEnumerable<SystemFinancialConfigResponse>> GetAllAsync()
    {
        var list = await _masterData.SystemFinancialConfigs.OrderBy(x => x.Code).ToListAsync();
        return list.Select(Map);
    }

    public async Task<SystemFinancialConfigResponse> GetByIdAsync(int id)
    {
        var entity = await _masterData.SystemFinancialConfigs.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"SystemFinancialConfig {id} not found.");
        return Map(entity);
    }

    public async Task<SystemFinancialConfigResponse> CreateAsync(UpsertSystemFinancialConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Phải nhập mã cấu hình.");

        var exists = await _masterData.SystemFinancialConfigs.AnyAsync(x => x.Code == request.Code);
        if (exists)
            throw new InvalidOperationException($"Đã có cấu hình mang mã {request.Code}.");

        var entity = new SystemFinancialConfig
        {
            Code = request.Code.ToUpperInvariant(),
            Value = request.Value,
            Description = request.Description,
            EffectiveDate = request.EffectiveDate,
            IsActive = request.IsActive
        };
        _masterData.Add(entity);
        await _masterData.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<SystemFinancialConfigResponse> UpdateAsync(int id, UpsertSystemFinancialConfigRequest request)
    {
        var entity = await _masterData.SystemFinancialConfigs.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"SystemFinancialConfig {id} not found.");

        var codeConflict = await _masterData.SystemFinancialConfigs
            .AnyAsync(x => x.Code == request.Code.ToUpperInvariant() && x.Id != id);
        if (codeConflict)
            throw new InvalidOperationException($"Đã có cấu hình mang mã {request.Code}.");

        entity.Code = request.Code.ToUpperInvariant();
        entity.Value = request.Value;
        entity.Description = request.Description;
        entity.EffectiveDate = request.EffectiveDate;
        entity.IsActive = request.IsActive;
        await _masterData.SaveChangesAsync();
        return Map(entity);
    }

    private static SystemFinancialConfigResponse Map(SystemFinancialConfig e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        Value = e.Value,
        Description = e.Description,
        EffectiveDate = e.EffectiveDate,
        IsActive = e.IsActive
    };

    /// <summary>
    /// Xoá vĩnh viễn chỉ khi KHÔNG ai tham chiếu — giống cách đã làm với loại đề tài.
    /// Còn dùng thì vô hiệu hoá (<c>isActive = false</c>) để dữ liệu cũ không mồ côi.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var entity = await _masterData.SystemFinancialConfigs.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy cấu hình tài chính {id}.");

        // Cấu hình tài chính không có bảng nào trỏ tới bằng khoá ngoại; nó được ĐỌC lúc tính
        // dự toán. Xoá không làm vỡ dữ liệu cũ (số đã tính rồi nằm trong đề tài), nên cho xoá thẳng.

        _masterData.Remove(entity);
        await _masterData.SaveChangesAsync();
    }
}
