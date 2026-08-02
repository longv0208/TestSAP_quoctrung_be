namespace FURPMS.Domain.Entities.Financial;

public class RubricTemplate
{
    public int Id { get; set; }
    public string TemplateType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal MaxTotalScore { get; set; } = 100m;
    public int? TrackId { get; set; }    // Review 2 (d): template tiêu chí scope theo track — nullable
    public int? OrderId { get; set; }    // Review 2 (d): scope theo research order — nullable
    public bool IsActive { get; set; } = true;

    // "Bộ tiêu chí" áp cho loại đề tài nào (thầy 29/07: tách tiêu chí theo Ứng dụng / Cơ bản).
    // Dùng làm RÀO CHẮN khi gán bộ vào đợt — đợt đã gắn chặt 1 loại nên chỉ gợi ý đợt đúng loại.
    public bool AppliesBasic { get; set; } = true;
    public bool AppliesApplied { get; set; } = true;

    public ICollection<RubricCriterion> Criteria { get; set; } = new List<RubricCriterion>();
    public ICollection<RubricTemplateScope> Scopes { get; set; } = new List<RubricTemplateScope>();
}

/// <summary>
/// Phạm vi áp dụng của 1 bộ tiêu chí: gắn vào (đợt + lĩnh vực). 1 bộ dùng lại cho NHIỀU đợt.
/// Ràng buộc nghiệp vụ: mỗi (đợt + lĩnh vực + LOẠI VÒNG) chỉ 1 bộ — nhưng cùng lĩnh vực vẫn có
/// bộ riêng cho Xét duyệt và bộ riêng cho Nghiệm thu (tiêu chí khác hẳn nhau).
/// </summary>
public class RubricTemplateScope
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int CycleId { get; set; }
    public int TrackId { get; set; }

    public RubricTemplate Template { get; set; } = null!;
}
