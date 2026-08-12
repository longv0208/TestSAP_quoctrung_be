namespace FURPMS.Application.Constants;

/// <summary>
/// Đổi mã trạng thái sang chữ tiếng Việt <b>dùng trong thông báo lỗi</b>.
/// <para>
/// Thông báo lỗi do máy chủ sinh ra thì giao diện không dịch lại được — nội suy thẳng
/// <c>{report.Status}</c> vào câu tiếng Việt là người dùng đọc phải *"Báo cáo đang ở trạng thái
/// EVALUATED"*. Đây đúng là lỗi lẫn ngôn ngữ thầy bắt lúc demo 05/08, chỉ khác chỗ xuất hiện.
/// </para>
/// <para>
/// Bảng này soi gương bảng <c>status.*</c> ở <c>src/i18n/locales/vi.ts</c> — thêm trạng thái mới
/// thì thêm cả hai chỗ. Mã lạ thì trả nguyên mã (lộ ra để còn biết mà bổ sung, hơn là hiện trống).
/// </para>
/// </summary>
public static class StatusText
{
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ACCEPTANCE"] = "Nghiệm thu",
        ["ACCEPTED"] = "Đã tiếp nhận",
        ["ACTIVE"] = "Đang hiệu lực",
        ["APPROVED"] = "Đã duyệt",
        ["ARCHIVED"] = "Đã lưu trữ",
        ["ASSIGNED"] = "Đã phân công",
        ["CANCELLED"] = "Đã huỷ",
        ["CLOSED"] = "Đã đóng",
        ["COMPLETED"] = "Hoàn thành",
        ["CONDITIONAL"] = "Đạt có điều kiện",
        ["CONFIRMED"] = "Đã xác nhận",
        ["DECIDED"] = "Đã chốt",
        ["DECLINED"] = "Đã từ chối",
        ["DISBURSED"] = "Đã giải ngân",
        ["DRAFT"] = "Bản nháp",
        ["EVALUATED"] = "Đã đánh giá",
        ["EXPIRED"] = "Quá hạn",
        ["FAIL"] = "Không đạt",
        ["FAILED"] = "Không đạt",
        ["FORMING"] = "Đang lập",
        ["INVITED"] = "Đã gửi lời mời",
        ["IN_PROGRESS"] = "Đang thực hiện",
        ["NEEDS_IMPROVEMENT"] = "Cần cải thiện",
        ["OPEN"] = "Đang mở",
        ["PASS"] = "Đạt",
        ["PASSED"] = "Đạt",
        ["PENDING"] = "Chờ xử lý",
        ["PENDING_SIGNATURE"] = "Chờ ký",
        ["PLANNED"] = "Đã lên kế hoạch",
        ["PLANNING"] = "Đang lên kế hoạch",
        ["PROPOSED"] = "Mới đề xuất",
        ["REJECTED"] = "Không duyệt",
        ["REOPENED"] = "Đã mở lại",
        ["REVISION_REQUIRED"] = "Cần chỉnh sửa",
        ["SATISFACTORY"] = "Đạt",
        ["SCHEDULED"] = "Đã lên lịch",
        ["SIGNED"] = "Đã ký",
        ["SUBMITTED"] = "Đã nộp",
        ["TERMINATED"] = "Đã chấm dứt",
        ["UNDER_REVIEW"] = "Đang xét duyệt",
        ["UNSATISFACTORY"] = "Không đạt",
    };

    /// <summary>Nhãn tiếng Việt của mã trạng thái; mã lạ trả về nguyên mã.</summary>
    public static string Vi(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "(chưa có)";
        return Labels.TryGetValue(status.Trim(), out var label) ? label : status;
    }
}
