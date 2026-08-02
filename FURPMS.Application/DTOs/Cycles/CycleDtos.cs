namespace FURPMS.Application.DTOs.Cycles;

public class TrackDto
{
    public string Id { get; set; } = null!;
    public string CycleId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResearchTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal MaxBudgetCap { get; set; }
    public bool RequireOrderingUnit { get; set; }
    public bool IsActive { get; set; }
}

public class CreateResearchTypeRequest
{
    public string? Code { get; set; }   // optional — BE tự sinh từ Name nếu không gửi
    public string Name { get; set; } = null!;
    public decimal MaxBudgetCap { get; set; }
    public bool RequireOrderingUnit { get; set; }
}

public class UpdateResearchTypeRequest
{
    public string? Name { get; set; }
    public decimal? MaxBudgetCap { get; set; }
    public bool? RequireOrderingUnit { get; set; }
}

public class CycleDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string AcademicYear { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int ResearchTypeId { get; set; }
    public string ResearchTypeName { get; set; } = null!;
    public string SubmissionStartDate { get; set; } = null!;
    public string SubmissionDeadline { get; set; } = null!;   // HẠN HIỆU LỰC (sau gia hạn nếu có)
    public string? OriginalDeadline { get; set; }              // hạn gốc — chỉ set khi đã gia hạn
    public int ExtensionCount { get; set; }                    // số lần gia hạn (0 = chưa gia hạn)
    public decimal FundingCap { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TrackCount { get; set; }
    public List<TrackDto> Tracks { get; set; } = new();
}

public class CreateCycleRequest
{
    public string Name { get; set; } = null!;
    public string AcademicYear { get; set; } = null!;
    public int ResearchTypeId { get; set; }
    public string SubmissionStartDate { get; set; } = null!;
    public string SubmissionDeadline { get; set; } = null!;
    public string? Description { get; set; }
}

public class CreateTrackRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? OwnerId { get; set; }
}

public class UpdateTrackRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? OwnerId { get; set; }   // đổi người phụ trách ngay trong form Sửa (null = bỏ gán)
}

public class AssignTrackOwnerRequest
{
    public string? OwnerId { get; set; }
}

// Gia hạn deadline đợt (rule tuần 10) — ghi log, không ghi đè ngày gốc.
public class ExtendDeadlineRequest
{
    public string NewDeadline { get; set; } = null!;  // yyyy-MM-dd
    public string? Reason { get; set; }
}

public class DeadlineExtensionDto
{
    public Guid Id { get; set; }
    public string OldDeadline { get; set; } = null!;
    public string NewDeadline { get; set; } = null!;
    public string? Reason { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
