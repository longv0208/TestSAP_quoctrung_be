using FURPMS.Domain.Entities.Financial;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Application.Interfaces.Repositories;

public interface IMasterDataRepository
{
    IQueryable<PersonnelRoleType> PersonnelRoleTypes { get; }
    IQueryable<BudgetExpenseCategory> BudgetExpenseCategories { get; }
    IQueryable<SystemFinancialConfig> SystemFinancialConfigs { get; }
    IQueryable<SystemSetting> SystemSettings { get; }
    IQueryable<AmendmentCategory> AmendmentCategories { get; }
    IQueryable<DisbursementTemplate> DisbursementTemplates { get; }
    IQueryable<ResearchType> ResearchTypes { get; }
    IQueryable<OrganizationalUnit> OrganizationalUnits { get; }
    IQueryable<RubricTemplate> RubricTemplates { get; }
    IQueryable<RubricCriterion> RubricCriteria { get; }
    IQueryable<RubricTemplateScope> RubricTemplateScopes { get; }   // bộ tiêu chí ↔ (đợt + lĩnh vực)
    IQueryable<ProductCategory> ProductCategories { get; }
    IQueryable<AcademicProfile> AcademicProfiles { get; }
    IQueryable<AcademicWork> AcademicWorks { get; }
    void Add<T>(T entity) where T : class;
    void Update<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task SaveChangesAsync(CancellationToken ct = default);
}
