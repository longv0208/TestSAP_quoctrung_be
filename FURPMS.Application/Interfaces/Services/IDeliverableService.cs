using FURPMS.Application.DTOs.Contract;

namespace FURPMS.Application.Interfaces.Services;

public interface IDeliverableService
{
    Task<IEnumerable<DeliverableResponse>> GetByContractAsync(Guid contractId);
    Task<DeliverableResponse> SubmitAsync(int deliverableId, SubmitDeliverableRequest request, Guid submittedBy);
    Task<DeliverableResponse> EvaluateAsync(int deliverableId, EvaluateDeliverableRequest request, Guid evaluatedBy);
}
