using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Projects;

// Thực thể TRUNG TÂM (Review 2): 1 project = n proposal version + n contract
// + members + deliverables + 1 final report.
public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? ProjectCode { get; set; }
    public int CycleTrackId { get; set; }
    public int OrderId { get; set; }          // 100% project thuộc 1 order (order mặc định cho đề tài tự do)
    public Guid PiUserId { get; set; }
    public int HostingUnitId { get; set; }
    public int ResearchTypeId { get; set; }
    public string TitleVi { get; set; } = null!;
    public string? TitleEn { get; set; }
    public string Status { get; set; } = "PROPOSED";
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly PlannedEndDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CycleTrack CycleTrack { get; set; } = null!;
    public ResearchOrder Order { get; set; } = null!;
    public User PiUser { get; set; } = null!;
    public OrganizationalUnit HostingUnit { get; set; } = null!;
    public ResearchType ResearchType { get; set; } = null!;
    public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<ProjectDeliverable> Deliverables { get; set; } = new List<ProjectDeliverable>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public FinalReport? FinalReport { get; set; }
}
