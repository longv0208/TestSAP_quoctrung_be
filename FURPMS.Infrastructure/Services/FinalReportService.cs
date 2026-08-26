using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Progress;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class FinalReportService : IFinalReportService
{
    private readonly IContractRepository _contracts;
    private readonly IClock _clock;
    private readonly ISystemSettingService _settings;
    private readonly IDecisionLogger _decisions;

    public FinalReportService(IContractRepository contracts,
        IClock clock,
        ISystemSettingService settings,
        IDecisionLogger decisions)
    {
        _decisions = decisions;
        _contracts = contracts;
        _clock = clock;
        _settings = settings;
    }

    public async Task<FinalReportDto?> GetByContractAsync(Guid contractId)
    {
        var contract = await _contracts.Query()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        var report = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.ProjectId == contract.ProjectId);
        return report == null ? null : Map(report);
    }

    public async Task<FinalReportDto> SubmitAsync(Guid contractId, SubmitFinalReportRequest request, Guid userId)
    {
        var reportLocation = NormalizeDocumentLocation(request.ReportFileUrl, "báo cáo tổng kết", required: true)!;
        var summaryLocation = NormalizeDocumentLocation(request.SummaryFileUrl, "bản tóm tắt", required: false);

        var language = request.Language?.Trim().ToUpperInvariant();
        if (language is not "VI" and not "EN")
            throw new ArgumentException("Ngôn ngữ báo cáo chỉ được chọn Tiếng Việt hoặc Tiếng Anh.");

        var contract = await _contracts.Query()
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (contract.Project.PiUserId != userId)
            throw new ForbiddenException("Chỉ chủ nhiệm đề tài mới nộp được báo cáo tổng kết.");

        var existing = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.ProjectId == contract.ProjectId);

        if (existing != null)
        {
            if (existing.Status == FinalReportStatus.Accepted || existing.Status == FinalReportStatus.Archived)
                throw new InvalidOperationException($"Báo cáo tổng kết đang ở trạng thái {StatusText.Vi(existing.Status)} — không sửa được nữa.");

            // Resubmission after revision
            existing.ReportFileUrl = reportLocation;
            existing.SummaryFileUrl = summaryLocation;
            existing.Language = language;
            existing.FinalSubmittedAt = _clock.UtcNow;
            existing.Status = FinalReportStatus.Submitted;
            await _contracts.SaveChangesAsync();
            return Map(existing);
        }

        contract.Project.Status = ProjectStatus.Acceptance;
        contract.Project.UpdatedAt = DateTime.UtcNow;

        // QĐ543 Điều 11.2.a (nguyên văn): "Chủ nhiệm đề tài phải nộp báo cáo nghiệm thu cho Phòng
        // QLKH và Đơn vị chủ trì ÍT NHẤT 30 NGÀY TRƯỚC KHI KẾT THÚC đề tài."
        //
        // Cột `deadline` có trong bảng từ đầu nhưng CHƯA BAO GIỜ ĐƯỢC GHI — chỉ được đọc ra DTO, nên
        // luôn null và không màn nào hiện được hạn này. Ghi tại đây để dòng thời gian đề tài có mốc
        // thật, và scanner nhắc hạn có cái để quét.
        //
        // Số ngày lấy từ system_settings, không cắm vào code: Điều 11.2.a nói "ít nhất 30 ngày" —
        // trường có thể siết chặt hơn, không được nới lỏng hơn.
        var leadDays = await _settings.GetIntAsync(
            SystemSettingKeys.FinalReportLeadDays, SystemSettingKeys.DefaultFinalReportLeadDays);

        var report = new FinalReport
        {
            ProjectId = contract.ProjectId,
            ReportFileUrl = reportLocation,
            SummaryFileUrl = summaryLocation,
            Language = language,
            SubmittedAt = _clock.UtcNow,
            Deadline = contract.EndDate.AddDays(-leadDays),
            Status = FinalReportStatus.Submitted
        };

        await _contracts.AddFinalReportAsync(report);
        await _contracts.SaveChangesAsync();
        return Map(report);
    }

    public async Task<FinalReportDto> RequestRevisionAsync(Guid reportId, RequestRevisionRequest request, Guid staffId)
    {
        if (string.IsNullOrWhiteSpace(request.RevisionNotes))
            throw new ArgumentException("Phải ghi rõ nội dung cần chỉnh sửa.");

        var report = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException("Không tìm thấy báo cáo tổng kết.");

        if (report.Status != FinalReportStatus.Submitted)
            throw new InvalidOperationException($"Báo cáo đang ở trạng thái {StatusText.Vi(report.Status)} — chỉ yêu cầu chỉnh sửa được với báo cáo đã nộp.");

        report.Status = FinalReportStatus.RevisionRequired;
        report.RevisionNotes = request.RevisionNotes;
        report.RevisionRequestedAt = _clock.UtcNow;
        await _contracts.SaveChangesAsync();
        return Map(report);
    }

    public async Task<FinalReportDto> AcceptAsync(Guid reportId, Guid staffId)
    {
        var report = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException("Không tìm thấy báo cáo tổng kết.");

        if (report.Status != FinalReportStatus.Submitted)
            throw new InvalidOperationException($"Báo cáo đang ở trạng thái {StatusText.Vi(report.Status)} — chỉ chấp nhận được báo cáo đã nộp.");

        report.Status = FinalReportStatus.Accepted;
        // Trước 25/08 chỗ này hardcode "+3 tháng". Cẩm nang chống trượt xếp hardcode tham số nghiệp
        // vụ là nguyên nhân trượt phổ biến thứ 2, và câu hỏi kinh điển của hội đồng là "đổi con số
        // này rồi demo ngay đi" — nên đưa ra system_settings, Admin sửa được, hiệu lực ngay.
        var archivalDays = await _settings.GetIntAsync(
            SystemSettingKeys.ArchivalLeadDays, SystemSettingKeys.DefaultArchivalLeadDays);
        report.ArchivalDeadline = DateOnly.FromDateTime(_clock.UtcNow).AddDays(archivalDays);
        await _contracts.SaveChangesAsync();
        return Map(report);
    }

    public async Task<FinalReportDto> ArchiveAsync(Guid reportId, Guid staffId)
    {
        var report = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException("Không tìm thấy báo cáo tổng kết.");

        if (report.Status != FinalReportStatus.Accepted)
            throw new InvalidOperationException($"Báo cáo đang ở trạng thái {StatusText.Vi(report.Status)} — chỉ lưu trữ được báo cáo đã được chấp nhận.");

        // QĐ543 Điều 13: sau nghiệm thu phải nộp cả báo cáo đầy đủ và báo cáo tóm tắt
        // (bằng tiếng Việt hoặc tiếng Anh) trước khi hoàn tất lưu trữ.
        if (string.IsNullOrWhiteSpace(report.SummaryFileUrl))
            throw new InvalidOperationException(
                "Chưa có bản tóm tắt báo cáo tổng kết — phải nộp đủ báo cáo đầy đủ và bản tóm tắt trước khi lưu trữ.");

        report.Status = FinalReportStatus.Archived;
        report.ArchivedAt = _clock.UtcNow;

        _decisions.Log(
            report.ProjectId, DecisionTypes.FinalReportApproved,
            "Duyệt và lưu trữ báo cáo tổng kết đề tài",
            "FinalReport", report.Id.ToString(),
            result: report.Status, documentNo: "BM12", decidedByRole: "Phòng QLKH");

        await _contracts.SaveChangesAsync();
        return Map(report);
    }

    private FinalReportDto Map(FinalReport r) => new()
    {
        Id = r.Id,
        ProjectId = r.ProjectId,
        Status = r.Status,
        ReportFileUrl = r.ReportFileUrl,
        SummaryFileUrl = r.SummaryFileUrl,
        Language = r.Language,
        Deadline = r.Deadline?.ToString("yyyy-MM-dd"),
        DaysLeft = DeadlineMath.DaysLeft(r.Deadline, _clock),
        SubmittedAt = r.SubmittedAt,
        RevisionNotes = r.RevisionNotes,
        RevisionRequestedAt = r.RevisionRequestedAt,
        FinalSubmittedAt = r.FinalSubmittedAt,
        ArchivalDeadline = r.ArchivalDeadline?.ToString("yyyy-MM-dd"),
        ArchivalDaysLeft = DeadlineMath.DaysLeft(r.ArchivalDeadline, _clock),
        ArchivedAt = r.ArchivedAt
    };

    private static string? NormalizeDocumentLocation(string? value, string label, bool required)
    {
        var location = value?.Trim();
        if (string.IsNullOrWhiteSpace(location))
        {
            if (required)
                throw new ArgumentException($"Phải tải file hoặc dán đường dẫn {label}.");
            return null;
        }

        // File tải lên hệ thống dùng endpoint có xác thực. Giữ dạng tương đối để FE mở bằng axios kèm JWT.
        if (location.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return location;

        if (Uri.TryCreate(location, UriKind.Absolute, out var absolute)
            && absolute.Scheme is "http" or "https")
            return absolute.ToString();

        // Cho phép người dùng dán dạng drive.google.com/... mà không cần tự gõ https://.
        if (Uri.TryCreate($"https://{location}", UriKind.Absolute, out var inferred)
            && (inferred.Host.Contains('.') || inferred.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)))
            return inferred.ToString();

        throw new ArgumentException(
            $"Đường dẫn {label} không hợp lệ — hãy tải file lên hoặc dán liên kết bắt đầu bằng http:// hoặc https://.");
    }
}
