using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IDecisionLogger"/>
public class DecisionLogger : IDecisionLogger
{
    private readonly FURPMSDbContext _db;
    private readonly IClock _clock;
    private readonly ILogger<DecisionLogger> _logger;

    public DecisionLogger(FURPMSDbContext db, IClock clock, ILogger<DecisionLogger> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public void Log(
        Guid projectId,
        string decisionType,
        string summary,
        string sourceEntityType,
        string sourceEntityId,
        string? result = null,
        string? reason = null,
        string? documentNo = null,
        Guid? decidedBy = null,
        string? decidedByRole = null,
        DateTime? decidedAt = null)
    {
        try
        {
            _db.ProjectDecisions.Add(new ProjectDecision
            {
                ProjectId = projectId,
                DecisionType = decisionType,
                Result = result,
                // Cắt cho vừa cột: một câu tóm tắt dài bất thường (tên đề tài rất dài) không được
                // làm hỏng cả thao tác nghiệp vụ chỉ vì tràn varchar.
                Summary = Truncate(summary, 1000),
                Reason = Truncate(reason, 2000),
                DocumentNo = Truncate(documentNo, 50),
                SourceEntityType = sourceEntityType,
                SourceEntityId = sourceEntityId,
                DecidedBy = decidedBy,
                DecidedByRole = Truncate(decidedByRole, 100),
                DecidedAt = decidedAt ?? _clock.UtcNow,
                CreatedAt = _clock.UtcNow
            });
        }
        catch (Exception ex)
        {
            // Nuốt có chủ đích. Sổ quyết định là bản ghi PHỤ; chặn một kết luận hợp lệ của hội đồng
            // chỉ vì không ghi được một dòng sổ là đánh đổi sai hướng. Lỗi vẫn vào log để còn biết.
            _logger.LogError(ex,
                "Không ghi được quyết định {DecisionType} cho đề tài {ProjectId}", decisionType, projectId);
        }
    }

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
