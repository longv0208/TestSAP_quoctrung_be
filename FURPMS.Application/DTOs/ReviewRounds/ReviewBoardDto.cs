using FURPMS.Application.DTOs.Councils;

namespace FURPMS.Application.DTOs.ReviewRounds;

// 1 phát trả toàn bộ dữ liệu màn "Hội đồng & Chấm" của 1 lĩnh vực trong 1 đợt:
// đề tài của track + các vòng của track + hội đồng trong từng vòng.
public class ReviewBoardDto
{
    public List<ReviewBoardProjectDto> Projects { get; set; } = new();
    public List<ReviewBoardRoundDto> Rounds { get; set; } = new();
}

public class ReviewBoardProjectDto
{
    public Guid ProjectId { get; set; }
    public Guid ProposalId { get; set; }
    public string TitleVi { get; set; } = null!;
    public string ProjectStatus { get; set; } = null!;
}

public class ReviewBoardRoundDto
{
    public Guid Id { get; set; }
    public int RoundNumber { get; set; }
    public string Dimension { get; set; } = null!;
    public string RoundType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Result { get; set; }
    public int? RubricTemplateId { get; set; }   // bộ tiêu chí gắn RIÊNG cho vòng này (null = theo đợt/lĩnh vực)
    public bool CanDelete { get; set; }
    public List<ReviewBoardProjectRoundDto> Projects { get; set; } = new();
    public List<ReviewBoardCouncilDto> Councils { get; set; } = new();
}

public class ReviewBoardProjectRoundDto
{
    public Guid ProjectId { get; set; }
    public string TitleVi { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Result { get; set; }
}

public class ReviewBoardCouncilDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = null!;
    public List<Guid> ProjectIds { get; set; } = new();
    public IList<CouncilMemberResponse> Members { get; set; } = new List<CouncilMemberResponse>();
}
