using FURPMS.Domain.Entities.Financial;
using FURPMS.Domain.Entities.Review;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Tests.Helpers;

/// <summary>
/// Dựng phiếu chấm <b>có điểm thật</b> cho test.
///
/// <para><b>Vì sao có (25/08):</b> trước đây test chỉ tạo <c>ProposalReviewScore</c> trống — đủ để
/// qua điều kiện 2/3 thành viên chấm của QĐ543 Điều 8.3.b, nhưng <b>không có dòng điểm nào</b>, nên
/// điểm trung bình tính ra 0. Ngoài đời chuyện đó không xảy ra: <c>SubmitScoreAsync</c> bắt buộc
/// chấm đủ mọi tiêu chí mới cho nộp phiếu.</para>
///
/// <para>Sự chênh lệch ấy nằm im cho tới khi thêm cảnh báo kết luận lệch điểm — lúc đó 15 test
/// chuyển đỏ vì hệ thống thấy "trung bình 0/100 mà kết luận Đạt". Hệ thống đúng, dữ liệu test sai.
/// Xưởng này để test dựng phiếu giống thật, đừng quay lại tạo phiếu rỗng.</para>
/// </summary>
public static class TestBallots
{
    public const int TemplateId = 1;
    public const decimal MaxTotal = 100m;

    /// <summary>Điểm mặc định — trên ngưỡng đạt, để test nào không quan tâm tới điểm thì khỏi lo.</summary>
    public const decimal PassingScore = 80m;

    /// <summary>Điểm thấp dùng cho test cảnh báo lệch.</summary>
    public const decimal LowScore = 35m;

    private const int CriterionA = 901;
    private const int CriterionB = 902;

    /// <summary>Bộ tiêu chí /100 gồm 2 mục (70 + 30). Gọi nhiều lần cũng không tạo trùng.</summary>
    public static void EnsureRubric(FURPMSDbContext db)
    {
        if (db.RubricTemplates.Any(t => t.Id == TemplateId)) return;

        db.RubricTemplates.Add(new RubricTemplate
        {
            Id = TemplateId,
            TemplateType = "REVIEW",
            Name = "Phiếu chấm dùng cho test",
            MaxTotalScore = MaxTotal,
            IsActive = true
        });
        db.RubricCriteria.AddRange(
            new RubricCriterion
            {
                Id = CriterionA, TemplateId = TemplateId,
                CriterionName = "Tiêu chí A", MaxScore = 70m, Sequence = 1, IsActive = true
            },
            new RubricCriterion
            {
                Id = CriterionB, TemplateId = TemplateId,
                CriterionName = "Tiêu chí B", MaxScore = 30m, Sequence = 2, IsActive = true
            });
        db.SaveChanges();
    }

    /// <summary>
    /// Gỡ phiếu kèm các dòng điểm của nó.
    ///
    /// <para>Xoá mỗi bản ghi phiếu mà bỏ lại dòng điểm thì EF báo quan hệ bị "severed". Ngoài đời
    /// không luồng nào xoá phiếu cả — chỉ test dựng tình huống "thành viên bị thay" mới cần.</para>
    /// </summary>
    public static void RemoveWithDetails(FURPMSDbContext db, params ProposalReviewScore[] scores)
    {
        var ids = scores.Select(x => x.Id).ToList();
        db.ReviewScoreDetails.RemoveRange(db.ReviewScoreDetails.Where(d => ids.Contains(d.ScoreId)));
        db.ProposalReviewScores.RemoveRange(scores);
        db.SaveChanges();
    }

    /// <summary>
    /// Một phiếu đã nộp, chấm <paramref name="totalScore"/> trên thang 100, chia vào 2 tiêu chí.
    /// </summary>
    public static ProposalReviewScore Add(
        FURPMSDbContext db, Guid councilId, Guid projectId, Guid evaluatorMemberId,
        decimal totalScore)
    {
        EnsureRubric(db);

        var score = new ProposalReviewScore
        {
            CouncilId = councilId,
            ProjectId = projectId,
            EvaluatorMemberId = evaluatorMemberId,
            TemplateId = TemplateId,
            SubmittedAt = DateTime.UtcNow,
            IsValidBallot = true
        };
        db.ProposalReviewScores.Add(score);
        db.SaveChanges();   // cần Id thật để gắn dòng điểm

        // Dồn vào tiêu chí A trước cho tới trần 70, phần dư sang B — cách chia không quan trọng,
        // miễn TỔNG đúng bằng điểm test muốn.
        var a = Math.Min(totalScore, 70m);
        var b = totalScore - a;
        db.ReviewScoreDetails.AddRange(
            new ReviewScoreDetail { ScoreId = score.Id, CriterionId = CriterionA, GivenScore = a },
            new ReviewScoreDetail { ScoreId = score.Id, CriterionId = CriterionB, GivenScore = b });
        db.SaveChanges();

        return score;
    }
}
