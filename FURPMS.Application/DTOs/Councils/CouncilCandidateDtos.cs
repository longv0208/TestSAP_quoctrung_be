namespace FURPMS.Application.DTOs.Councils;

/// <summary>
/// Một ứng viên ủy viên hội đồng, kèm đủ thứ Phòng QLKH cần để quyết định ngay trên danh sách.
/// </summary>
public class CouncilCandidateDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? AcademicTitle { get; set; }
    public string? UnitName { get; set; }

    /// <summary>Các lĩnh vực người này đã khai.</summary>
    public List<string> Tracks { get; set; } = new();

    /// <summary>Có làm đúng lĩnh vực của đề tài không (QĐ543 Điều 8.2).</summary>
    public bool MatchesTrack { get; set; }

    /// <summary>
    /// Chưa khai lĩnh vực nào — <b>khác</b> với "khác lĩnh vực".
    ///
    /// <para>Gộp hai thứ này làm một là oan cho người chưa được ai nhập hồ sơ: họ bị đẩy xuống cuối
    /// như thể đã xác định là sai chuyên môn, trong khi thực ra hệ thống chỉ đang không biết.</para>
    /// </summary>
    public bool ExpertiseUnknown { get; set; }

    /// <summary>Xung đột lợi ích với đề tài — chủ nhiệm hoặc thành viên nhóm (rule #5).</summary>
    public bool HasConflictOfInterest { get; set; }

    /// <summary>Đã có tên trong hội đồng này rồi.</summary>
    public bool AlreadyInCouncil { get; set; }

    /// <summary>Đang là ủy viên của bao nhiêu hội đồng khác còn hoạt động — để tránh dồn việc một người.</summary>
    public int ActiveCouncilCount { get; set; }
}

/// <summary>Danh sách ứng viên kèm ngữ cảnh đề tài đang lập hội đồng.</summary>
public class CouncilCandidatesResponse
{
    public int? TrackId { get; set; }
    public string? TrackName { get; set; }
    public int MatchingCount { get; set; }
    public int TotalCount { get; set; }
    public List<CouncilCandidateDto> Candidates { get; set; } = new();
}
