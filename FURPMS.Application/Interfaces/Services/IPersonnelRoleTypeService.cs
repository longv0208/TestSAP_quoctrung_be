using FURPMS.Application.DTOs.MasterData;

namespace FURPMS.Application.Interfaces.Services;

public interface IPersonnelRoleTypeService
{
    Task<IEnumerable<PersonnelRoleTypeResponse>> GetAllAsync();
    Task<PersonnelRoleTypeResponse> GetByIdAsync(int id);
    Task<PersonnelRoleTypeResponse> CreateAsync(UpsertPersonnelRoleTypeRequest request);
    Task<PersonnelRoleTypeResponse> UpdateAsync(int id, UpsertPersonnelRoleTypeRequest request);
}
