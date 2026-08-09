using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class PersonnelRoleTypeService : IPersonnelRoleTypeService
{
    private readonly IMasterDataRepository _masterData;
    // Kiểm tham chiếu chéo sang bảng nghiệp vụ ⇒ đọc thẳng DbContext.
    private readonly FURPMSDbContext _db;

    public PersonnelRoleTypeService(IMasterDataRepository masterData, FURPMSDbContext db)
    {
        _masterData = masterData;
        _db = db;
    }

    public async Task<IEnumerable<PersonnelRoleTypeResponse>> GetAllAsync()
    {
        var list = await _masterData.PersonnelRoleTypes.OrderBy(x => x.Code).ToListAsync();
        return list.Select(Map);
    }

    public async Task<PersonnelRoleTypeResponse> GetByIdAsync(int id)
    {
        var entity = await _masterData.PersonnelRoleTypes.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"PersonnelRoleType {id} not found.");
        return Map(entity);
    }

    public async Task<PersonnelRoleTypeResponse> CreateAsync(UpsertPersonnelRoleTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Phải nhập mã chức danh.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Phải nhập tên chức danh.");

        var exists = await _masterData.PersonnelRoleTypes.AnyAsync(x => x.Code == request.Code);
        if (exists)
            throw new InvalidOperationException($"Đã có chức danh mang mã {request.Code}.");

        var entity = new PersonnelRoleType
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            DefaultCoefficient = request.DefaultCoefficient,
            IsActive = request.IsActive
        };
        _masterData.Add(entity);
        await _masterData.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<PersonnelRoleTypeResponse> UpdateAsync(int id, UpsertPersonnelRoleTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Phải nhập tên chức danh.");

        var entity = await _masterData.PersonnelRoleTypes.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"PersonnelRoleType {id} not found.");

        var codeConflict = await _masterData.PersonnelRoleTypes
            .AnyAsync(x => x.Code == request.Code.ToUpperInvariant() && x.Id != id);
        if (codeConflict)
            throw new InvalidOperationException($"Đã có chức danh mang mã {request.Code}.");

        entity.Code = request.Code.ToUpperInvariant();
        entity.Name = request.Name;
        entity.DefaultCoefficient = request.DefaultCoefficient;
        entity.IsActive = request.IsActive;
        await _masterData.SaveChangesAsync();
        return Map(entity);
    }

    private static PersonnelRoleTypeResponse Map(PersonnelRoleType e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        Name = e.Name,
        DefaultCoefficient = e.DefaultCoefficient,
        IsActive = e.IsActive
    };

    /// <summary>
    /// Xoá vĩnh viễn chỉ khi KHÔNG ai tham chiếu — giống cách đã làm với loại đề tài.
    /// Còn dùng thì vô hiệu hoá (<c>isActive = false</c>) để dữ liệu cũ không mồ côi.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var entity = await _masterData.PersonnelRoleTypes.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy vai trò nhân sự {id}.");

        var used = await _db.ProjectMembers.AnyAsync(m => m.MemberRoleCode == entity.Code);
        if (used)
            throw new InvalidOperationException(
                $"Vai trò \"{entity.Name}\" đang được thành viên đề tài sử dụng — " +
                "chỉ có thể vô hiệu hoá, không xoá vĩnh viễn được.");

        _masterData.Remove(entity);
        await _masterData.SaveChangesAsync();
    }
}
