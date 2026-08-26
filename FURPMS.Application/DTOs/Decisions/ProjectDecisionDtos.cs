namespace FURPMS.Application.DTOs.Decisions;

/// <summary>Một dòng trong hồ sơ quyết định của đề tài.</summary>
public class ProjectDecisionDto
{
    public Guid Id { get; set; }
    public string DecisionType { get; set; } = null!;
    /// <summary>Chặng của quyết định — giao diện gom theo đây thay vì đổ ra một danh sách phẳng.</summary>
    public string Stage { get; set; } = null!;
    public string? Result { get; set; }
    public string Summary { get; set; } = null!;
    public string? Reason { get; set; }
    public string? DocumentNo { get; set; }

    /// <summary>Bảng gốc + khoá, để giao diện dựng link "Xem bản gốc".</summary>
    public string SourceEntityType { get; set; } = null!;
    public string SourceEntityId { get; set; } = null!;

    public Guid? DecidedBy { get; set; }
    public string? DecidedByName { get; set; }
    /// <summary>Chức danh <b>tại thời điểm chốt</b> — không phải vai hiện tại của người đó.</summary>
    public string? DecidedByRole { get; set; }
    public DateTime DecidedAt { get; set; }

    /// <summary>File đính kèm của quyết định (<c>documents</c> với <c>EntityType = "ProjectDecision"</c>).</summary>
    public List<DecisionAttachmentDto> Attachments { get; set; } = new();
}

public class DecisionAttachmentDto
{
    public Guid Id { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? DocumentType { get; set; }
    public DateTime? UploadedAt { get; set; }
}

/// <summary>Toàn bộ hồ sơ quyết định của một đề tài.</summary>
public class ProjectDecisionDossierResponse
{
    public Guid ProjectId { get; set; }
    public string? ProjectCode { get; set; }
    public string? TitleVi { get; set; }
    public int TotalCount { get; set; }
    public List<ProjectDecisionDto> Decisions { get; set; } = new();
}

/// <summary>Kết quả một lần chạy dựng lại hồ sơ từ dữ liệu đã có.</summary>
public class BackfillDecisionsResponse
{
    /// <summary>Số dòng thực sự được thêm mới lần này.</summary>
    public int Created { get; set; }
    /// <summary>Số quyết định đã có sẵn trong sổ nên bỏ qua — chạy lại lần hai phải ra toàn số này.</summary>
    public int Skipped { get; set; }
    public int ProjectsScanned { get; set; }
    /// <summary>Chạy thử: đếm nhưng KHÔNG ghi.</summary>
    public bool DryRun { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
}
