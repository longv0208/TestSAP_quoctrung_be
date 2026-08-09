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
    void RemoveDeliverable(ProjectDeliverable deliverable);
    Task AddProgressReportAsync(ProgressReport report);
    void AddProgressReportItemsRange(IEnumerable<ProgressReportItem> items);
    void RemoveProgressReportItemsRange(IEnumerable<ProgressReportItem> items);
    // Xoá hợp đồng nhập nhầm: các bảng con do hệ thống tự sinh phải dọn theo, không có FK cascade.
    void RemoveDisbursementsRange(IEnumerable<ContractDisbursement> items);
    void RemoveProgressReportsRange(IEnumerable<ProgressReport> items);
    Task AddFinalReportAsync(FinalReport report);
    Task AddSettlementAsync(ContractSettlement settlement);
}
