using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Projects;

namespace FURPMS.Application.Interfaces.Repositories;

public interface IContractRepository : IRepository<Contract>
{
    IQueryable<ContractPhase> Phases { get; }
    IQueryable<ContractDisbursement> Disbursements { get; }
    IQueryable<ProjectDeliverable> Deliverables { get; }
    IQueryable<AmendmentRequest> Amendments { get; }
    IQueryable<ContractSettlement> Settlements { get; }
    IQueryable<ProgressReport> ProgressReports { get; }
    IQueryable<ProgressReportItem> ProgressReportItems { get; }
    IQueryable<FinalReport> FinalReports { get; }

    void AddPhase(ContractPhase phase);
    void AddDisbursementsRange(IEnumerable<ContractDisbursement> items);
    Task AddAmendmentAsync(AmendmentRequest amendment);
    void AddDeliverable(ProjectDeliverable deliverable);
    Task AddProgressReportAsync(ProgressReport report);
    void AddProgressReportItemsRange(IEnumerable<ProgressReportItem> items);
    Task AddFinalReportAsync(FinalReport report);
    Task AddSettlementAsync(ContractSettlement settlement);
}
