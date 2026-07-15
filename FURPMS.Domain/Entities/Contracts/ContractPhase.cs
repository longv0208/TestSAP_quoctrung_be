namespace FURPMS.Domain.Entities.Contracts;

// Review 2 điểm (e): giai đoạn thực hiện trong hợp đồng; deliverable + tranche giải ngân gắn vào phase.
public class ContractPhase
{
    public int Id { get; set; }
    public Guid ContractId { get; set; }
    public int PhaseNo { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "PLANNED";

    public Contract Contract { get; set; } = null!;
}
