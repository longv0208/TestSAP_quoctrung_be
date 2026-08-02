using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Financial;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Infrastructure.Repositories;

public class MasterDataRepository : IMasterDataRepository
{
    private readonly FURPMSDbContext _db;

    public MasterDataRepository(FURPMSDbContext db) => _db = db;

    public IQueryable<PersonnelRoleType> PersonnelRoleTypes => _db.PersonnelRoleTypes;
    public IQueryable<BudgetExpenseCategory> BudgetExpenseCategories => _db.BudgetExpenseCategories;
    public IQueryable<SystemFinancialConfig> SystemFinancialConfigs => _db.SystemFinancialConfigs;
    public IQueryable<SystemSetting> SystemSettings => _db.SystemSettings;
    public IQueryable<AmendmentCategory> AmendmentCategories => _db.AmendmentCategories;
    public IQueryable<DisbursementTemplate> DisbursementTemplates => _db.DisbursementTemplates;
    public IQueryable<ResearchType> ResearchTypes => _db.ResearchTypes;
    public IQueryable<OrganizationalUnit> OrganizationalUnits => _db.OrganizationalUnits;
    public IQueryable<RubricTemplate> RubricTemplates => _db.RubricTemplates;
    public IQueryable<RubricCriterion> RubricCriteria => _db.RubricCriteria;
    public IQueryable<RubricTemplateScope> RubricTemplateScopes => _db.RubricTemplateScopes;
    public IQueryable<ProductCategory> ProductCategories => _db.ProductCategories;
    public IQueryable<AcademicProfile> AcademicProfiles => _db.AcademicProfiles;

    public void Add<T>(T entity) where T : class => _db.Set<T>().Add(entity);
    public void Update<T>(T entity) where T : class => _db.Set<T>().Update(entity);
    public void Remove<T>(T entity) where T : class => _db.Set<T>().Remove(entity);
    public async Task SaveChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);
}
