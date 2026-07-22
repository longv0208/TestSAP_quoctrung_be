using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.MasterData;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class SystemFinancialConfigService : ISystemFinancialConfigService
{
    private readonly IMasterDataRepository _masterData;

    public SystemFinancialConfigService(IMasterDataRepository masterData)
    {
        _masterData = masterData;
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
            throw new ArgumentException("Code is required.");

        var exists = await _masterData.SystemFinancialConfigs.AnyAsync(x => x.Code == request.Code);
        if (exists)
            throw new InvalidOperationException($"SystemFinancialConfig with code '{request.Code}' already exists.");

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
            throw new InvalidOperationException($"SystemFinancialConfig with code '{request.Code}' already exists.");

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
}
