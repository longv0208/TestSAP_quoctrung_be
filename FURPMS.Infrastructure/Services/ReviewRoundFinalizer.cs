using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Review;

namespace FURPMS.Infrastructure.Services;

// Nối kết quả chấm cho TỪNG đề tài trong 1 round + tự đóng round. Gom về 1 chỗ để dùng chung
// cho ReviewRoundService.CloseRoundAsync (chốt thủ công) và ReviewScoringService.ApproveMinutesAsync
// (Chủ tịch duyệt biên bản) — tránh 2 đường tính lệch nhau (trước đây là nguồn bug).
internal static class ReviewRoundFinalizer
{
    // Áp kết quả cho 1 project_round.
    // - APPROVED / REJECTED → khóa (FinalizedAt) + PASSED / FAILED (terminal).
    // - REVISION_REQUIRED → KHÔNG terminal: giữ vòng OPEN, KHÔNG khóa, để PI sửa & nộp lại (rule #1);
    //   board hiển thị "đang mở / cần chỉnh sửa" thay vì "Từ chối".
    public static void ApplyProjectResult(ReviewRound round, ProjectRound projectRound, string result, DateTime now)
    {
        projectRound.Result = result;
        if (result == ReviewResult.RevisionRequired)
        {
            projectRound.Status = ReviewRoundStatus.Open;
            projectRound.FinalizedAt = null;
        }
        else
        {
            projectRound.Status = result == ReviewResult.Approved
                ? ReviewRoundStatus.Passed
                : ReviewRoundStatus.Failed;
            projectRound.FinalizedAt = now;
        }

        CloseRoundIfAllDecided(round, now);
    }

    // Round chung chỉ đóng khi MỌI đề tài đã CHỐT terminal (PASSED/FAILED). Đề tài REVISION còn treo → chưa đóng.
    // Status/Result đều suy từ "tất cả PASSED?" nên nhất quán và KHÔNG phụ thuộc thứ tự duyệt.
    public static void CloseRoundIfAllDecided(ReviewRound round, DateTime now)
    {
        if (round.ProjectRounds.Count == 0) return;

        var allTerminal = round.ProjectRounds.All(pr =>
            pr.Status == ReviewRoundStatus.Passed || pr.Status == ReviewRoundStatus.Failed);
        if (!allTerminal) return;

        var allPassed = round.ProjectRounds.All(pr => pr.Status == ReviewRoundStatus.Passed);
        round.Status = allPassed ? ReviewRoundStatus.Passed : ReviewRoundStatus.Failed;
        round.Result = allPassed ? ReviewResult.Approved : ReviewResult.Rejected;
        round.ClosedAt = now;
    }
}
