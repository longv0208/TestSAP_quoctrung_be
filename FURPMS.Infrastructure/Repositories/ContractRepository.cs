using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Infrastructure.Repositories;

public class ContractRepository : Repository<Contract>, IContractRepository
{
    public ContractRepository(FURPMSDbContext db) : base(db) { }

    public IQueryable<ContractPhase> Phases => _db.ContractPhases;
    public IQueryable<ContractDisbursement> Disbursements => _db.ContractDisbursements;
    public IQueryable<ProjectDeliverable> Deliverables => _db.ProjectDeliverables;
    public IQueryable<AmendmentRequest> Amendments => _db.AmendmentRequests;
    public IQueryable<ContractSettlement> Settlements => _db.ContractSettlements;
    public IQueryable<ProgressReport> ProgressReports => _db.ProgressReports;
    public IQueryable<ProgressReportItem> ProgressReportItems => _db.ProgressReportItems;
    public IQueryable<FinalReport> FinalReports => _db.FinalReports;

    public void AddPhase(ContractPhase phase) => _db.ContractPhases.Add(phase);
    public void AddDisbursementsRange(IEnumerable<ContractDisbursement> items) => _db.ContractDisbursements.AddRange(items);
    public async Task AddAmendmentAsync(AmendmentRequest amendment) => await _db.AmendmentRequests.AddAsync(amendment);
    public void AddDeliverable(ProjectDeliverable deliverable) => _db.ProjectDeliverables.Add(deliverable);
    public async Task AddProgressReportAsync(ProgressReport report) => await _db.ProgressReports.AddAsync(report);
    public void AddProgressReportItemsRange(IEnumerable<ProgressReportItem> items) => _db.ProgressReportItems.AddRange(items);
    public async Task AddFinalReportAsync(FinalReport report) => await _db.FinalReports.AddAsync(report);
    public async Task AddSettlementAsync(ContractSettlement settlement) => await _db.ContractSettlements.AddAsync(settlement);
}
