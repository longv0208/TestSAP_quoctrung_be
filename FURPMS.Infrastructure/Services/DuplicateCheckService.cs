using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Ai;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IDuplicateCheckService"/>
public class DuplicateCheckService : IDuplicateCheckService
{
    public const string VectorEntityType = "Proposal";
    private const string DuplicateOutputType = "DUPLICATE_CHECK";
    private const string PromptVersion = "v1";

    /// <summary>Bao nhiêu cặp đưa cho tầng 2 mổ xẻ. Nhiều hơn 3 thì prompt dài mà đọc cũng không hết.</summary>
    private const int MaxPairsToExplain = 3;

    private readonly FURPMSDbContext _db;
    private readonly IGeminiService _gemini;
    private readonly ISystemSettingService _settings;
    private readonly IDecisionLogger _decisions;
    private readonly IClock _clock;
    private readonly ILogger<DuplicateCheckService> _logger;
    private readonly INotifier _notifier;

    public DuplicateCheckService(
        FURPMSDbContext db,
        IGeminiService gemini,
        ISystemSettingService settings,
        IDecisionLogger decisions,
        IClock clock,
        ILogger<DuplicateCheckService> logger,
        INotifier notifier)
    {
        _db = db;
        _gemini = gemini;
        _settings = settings;
        _decisions = decisions;
        _clock = clock;
        _logger = logger;
        _notifier = notifier;
    }

    // ══════════════════════════════════════════════════════════════════════
    // TẦNG 1 — VECTOR + COSINE (rẻ, tất định, chạy mọi lần)
    // ══════════════════════════════════════════════════════════════════════

    public async Task<DuplicateCheckResponse> GetAsync(
        Guid proposalId, Guid userId, IEnumerable<string> roles)
    {
        var proposal = await LoadProposalAsync(proposalId);
        await AssertCanViewAsync(proposal.ProjectId, proposal.PiUserId, userId, roles);

        var warn = await GetDecimalSettingAsync(
            SystemSettingKeys.AiDuplicateThreshold, SystemSettingKeys.DefaultAiDuplicateThreshold);
        var high = await GetDecimalSettingAsync(
            SystemSettingKeys.AiDuplicateBlockThreshold, SystemSettingKeys.DefaultAiDuplicateBlockThreshold);
        var topK = await _settings.GetIntAsync(
            SystemSettingKeys.AiDuplicateTopK, SystemSettingKeys.DefaultAiDuplicateTopK);

        var dto = new DuplicateCheckResponse
        {
            ProposalId = proposalId,
            TitleVi = proposal.TitleVi,
            WarnThreshold = warn,
            HighThreshold = high
        };

        var mine = await _db.SemanticSearchVectors
            .FirstOrDefaultAsync(v => v.EntityType == VectorEntityType && v.EntityId == proposalId.ToString());
        var myVector = VectorMath.Deserialize(mine?.Embedding);
        dto.Indexed = myVector.Length > 0;

        var others = await _db.SemanticSearchVectors
            .Where(v => v.EntityType == VectorEntityType
                        && v.EntityId != proposalId.ToString()
                        && v.Embedding != null)
            .Select(v => new { v.EntityId, v.Embedding })
            .ToListAsync();
        dto.CorpusSize = others.Count;

        if (dto.Indexed && others.Count > 0)
        {
            var scored = others
                .Select(o => new
                {
                    o.EntityId,
                    Score = VectorMath.CosineSimilarity(myVector, VectorMath.Deserialize(o.Embedding))
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            dto.Matches = await BuildMatchesAsync(scored.Select(x => (x.EntityId, x.Score)), warn, high);
        }

        await FillReviewStateAsync(dto, proposalId);
        return dto;
    }

    public async Task<Dictionary<Guid, DuplicateFlagDto>> GetFlagsAsync(IEnumerable<Guid> proposalIds)
    {
        var ids = proposalIds.Distinct().ToList();
        var result = ids.ToDictionary(id => id, _ => new DuplicateFlagDto());
        if (ids.Count == 0) return result;

        var warn = await GetDecimalSettingAsync(
            SystemSettingKeys.AiDuplicateThreshold, SystemSettingKeys.DefaultAiDuplicateThreshold);
        var high = await GetDecimalSettingAsync(
            SystemSettingKeys.AiDuplicateBlockThreshold, SystemSettingKeys.DefaultAiDuplicateBlockThreshold);

        // Một truy vấn cho MỌI vector đang có — kho cỡ vài trăm bản ghi nên tải hết vào bộ nhớ rồi
        // so chéo trong C# rẻ hơn nhiều so với N truy vấn (N = số đề cương trên trang danh sách).
        var idStrings = ids.Select(id => id.ToString()).ToHashSet();
        var all = await _db.SemanticSearchVectors
            .Where(v => v.EntityType == VectorEntityType && v.Embedding != null)
            .Select(v => new { v.EntityId, v.Embedding })
            .ToListAsync();

        var vectorsById = all.ToDictionary(v => v.EntityId, v => VectorMath.Deserialize(v.Embedding));

        foreach (var id in ids)
        {
            var key = id.ToString();
            if (!vectorsById.TryGetValue(key, out var mine) || mine.Length == 0) continue;

            var best = 0d;
            foreach (var (otherId, otherVec) in vectorsById)
            {
                if (otherId == key) continue;
                var sim = VectorMath.CosineSimilarity(mine, otherVec);
                if (sim > best) best = sim;
            }

            result[id] = new DuplicateFlagDto
            {
                Indexed = true,
                MaxSimilarity = best > 0 ? Math.Round(best, 4) : null,
                MaxSeverity = (decimal)best >= high ? DuplicateSeverity.High
                    : (decimal)best >= warn ? DuplicateSeverity.Warn
                    : null
            };
        }

        return result;
    }

    /// <summary>Ghép điểm số với thông tin đề tài để giao diện hiện được tên, chủ nhiệm, năm.</summary>
    private async Task<List<DuplicateMatchDto>> BuildMatchesAsync(
        IEnumerable<(string EntityId, double Score)> scored, decimal warn, decimal high)
    {
        var byId = scored.ToDictionary(
            x => Guid.TryParse(x.EntityId, out var g) ? g : Guid.Empty,
            x => x.Score);
        byId.Remove(Guid.Empty);
        if (byId.Count == 0) return new();

        var ids = byId.Keys.ToList();
        var rows = await _db.Proposals
            .IgnoreQueryFilters()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.ProjectId,
                p.TitleVi,
                ProjectCode = p.Project.ProjectCode,
                PiName = p.Project.PiUser.FullName,
                ProjectStatus = p.Project.Status,
                CycleYear = (int?)p.Project.CycleTrack.Cycle.CycleYear
            })
            .ToListAsync();

        return rows
            .Select(r =>
            {
                var score = byId[r.Id];
                return new DuplicateMatchDto
                {
                    ProposalId = r.Id,
                    ProjectId = r.ProjectId,
                    ProjectCode = r.ProjectCode,
                    TitleVi = r.TitleVi,
                    PiName = r.PiName,
                    ProjectStatus = r.ProjectStatus,
                    CycleYear = r.CycleYear,
                    Similarity = Math.Round(score, 4),
                    Severity = (decimal)score >= high ? DuplicateSeverity.High
                        : (decimal)score >= warn ? DuplicateSeverity.Warn
                        : DuplicateSeverity.Low
                };
            })
            .OrderByDescending(m => m.Similarity)
            .ToList();
    }

    // ══════════════════════════════════════════════════════════════════════
    // TẦNG 2 — GIẢI THÍCH (đắt, chỉ chạy khi cần)
    // ══════════════════════════════════════════════════════════════════════

    public async Task<DuplicateCheckResponse> ExplainAsync(
        Guid proposalId, Guid userId, IEnumerable<string> roles, bool force)
    {
        var dto = await GetAsync(proposalId, userId, roles);

        if (!dto.Indexed)
            throw new InvalidOperationException(
                "Đề cương này chưa được vector hoá nên chưa có gì để đối chiếu. " +
                "Hãy chạy lại việc lập chỉ mục (Quản trị → Rà trùng lặp) rồi thử lại.");

        var pairs = dto.Matches
            .Where(m => m.Severity != DuplicateSeverity.Low)
            .Take(MaxPairsToExplain)
            .ToList();

        if (pairs.Count == 0)
            throw new InvalidOperationException(
                "Không có đề tài nào vượt ngưỡng cảnh báo — chưa cần nhờ AI giải thích.");

        // Bản đã lưu còn dùng được thì dùng lại: mở lại hồ sơ lần thứ hai không được đốt quota.
        if (!force && dto.Explanation != null) return dto;

        if (!_gemini.IsConfigured)
            throw new InvalidOperationException(
                "Chưa cấu hình GeminiAI:ApiKey — tầng giải thích cần khoá API. " +
                "Danh sách đối chiếu ở trên vẫn dùng được bình thường.");

        var mine = await LoadProposalAsync(proposalId);
        var prompt = BuildPrompt(mine, pairs);

        var usage = await _gemini.GenerateWithUsageAsync(prompt);

        foreach (var old in await _db.LlmOutputs
                     .Where(o => o.EntityType == VectorEntityType && o.EntityId == proposalId.ToString()
                                 && o.OutputType == DuplicateOutputType && o.IsActive)
                     .ToListAsync())
            old.IsActive = false;

        _db.LlmOutputs.Add(new LlmOutput
        {
            EntityType = VectorEntityType,
            EntityId = proposalId.ToString(),
            OutputType = DuplicateOutputType,
            ModelUsed = usage.ModelUsed,
            PromptVersion = PromptVersion,
            Content = usage.Text,
            // Ba cột này có trong bảng từ đầu mà chưa luồng nào ghi. Không ghi thì không trả lời
            // được câu "nhóm có quản lý chi phí AI không".
            TokensInput = usage.TokensInput,
            TokensOutput = usage.TokensOutput,
            LatencyMs = usage.LatencyMs,
            GeneratedAt = _clock.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        dto.Explanation = usage.Text;
        dto.ExplanationGeneratedAt = _clock.UtcNow;
        dto.ExplanationModel = usage.ModelUsed;
        return dto;
    }

    private static string BuildPrompt(ProposalSnapshot mine, List<DuplicateMatchDto> pairs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý của Phòng Quản lý khoa học một trường đại học.");
        sb.AppendLine("Nhiệm vụ: so sánh ĐỀ CƯƠNG MỚI với các đề tài đã có, chỉ ra điểm trùng lặp cụ thể.");
        sb.AppendLine();
        sb.AppendLine("Yêu cầu về câu trả lời:");
        sb.AppendLine("- Viết tiếng Việt, tối đa 200 từ.");
        sb.AppendLine("- Với mỗi đề tài, nêu rõ TRÙNG Ở ĐÂU (mục tiêu / phương pháp / sản phẩm) và KHÁC Ở ĐÂU.");
        sb.AppendLine("- KHÔNG kết luận đề tài này có bị loại hay không — quyết định đó là của Phòng QLKH.");
        sb.AppendLine("- Nếu thực chất chỉ cùng chủ đề chứ không trùng nội dung, hãy nói thẳng như vậy.");
        sb.AppendLine();
        sb.AppendLine("=== ĐỀ CƯƠNG MỚI ===");
        sb.AppendLine($"Tên: {mine.TitleVi}");
        if (!string.IsNullOrWhiteSpace(mine.AbstractVi)) sb.AppendLine($"Tóm tắt: {Clip(mine.AbstractVi)}");
        if (!string.IsNullOrWhiteSpace(mine.ResearchObjectives)) sb.AppendLine($"Mục tiêu: {Clip(mine.ResearchObjectives)}");
        sb.AppendLine();

        foreach (var (p, i) in pairs.Select((p, i) => (p, i + 1)))
        {
            sb.AppendLine($"=== ĐỀ TÀI ĐÃ CÓ #{i} (độ tương đồng {p.Similarity:0.###}) ===");
            sb.AppendLine($"Tên: {p.TitleVi}");
            if (p.CycleYear is { } y) sb.AppendLine($"Năm: {y}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Clip(string? s, int max = 900) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "…";

    // ══════════════════════════════════════════════════════════════════════
    // NGƯỜI TRONG VÒNG LẶP
    // ══════════════════════════════════════════════════════════════════════

    public async Task<DuplicateCheckResponse> ReviewAsync(
        Guid proposalId, ReviewDuplicateRequest request, Guid reviewerId, IEnumerable<string> roles)
    {
        var valid = new[] { DuplicateVerdict.NotDuplicate, DuplicateVerdict.NeedsRevision, DuplicateVerdict.Duplicate };
        if (!valid.Contains(request.Verdict))
            throw new ArgumentException($"Kết luận chỉ nhận: {string.Join(", ", valid)}.");

        // Kết luận "trùng" hoặc "cần sửa" mà không nói vì sao thì chủ nhiệm không biết phải làm gì,
        // và hồ sơ sau này đọc lại cũng không hiểu.
        var note = request.Note?.Trim();
        if (request.Verdict != DuplicateVerdict.NotDuplicate && string.IsNullOrWhiteSpace(note))
            throw new ArgumentException(
                "Kết luận có vấn đề về trùng lặp thì phải ghi rõ căn cứ — chủ nhiệm cần biết phải sửa gì.");

        var proposal = await LoadProposalAsync(proposalId);
        await AssertCanViewAsync(proposal.ProjectId, proposal.PiUserId, reviewerId, roles);

        var output = await _db.LlmOutputs
            .FirstOrDefaultAsync(o => o.EntityType == VectorEntityType && o.EntityId == proposalId.ToString()
                                      && o.OutputType == DuplicateOutputType && o.IsActive);

        if (output is null)
        {
            // Chưa chạy tầng 2 thì vẫn kết luận được — Phòng QLKH có quyền đọc danh sách rồi
            // quyết luôn, không bắt họ gọi AI cho đủ thủ tục.
            output = new LlmOutput
            {
                EntityType = VectorEntityType,
                EntityId = proposalId.ToString(),
                OutputType = DuplicateOutputType,
                ModelUsed = "(không dùng AI)",
                PromptVersion = PromptVersion,
                Content = "",
                GeneratedAt = _clock.UtcNow,
                IsActive = true
            };
            // BẢN GHI MỚI → Add. Gọi Update() cho một thực thể EF chưa từng biết tới sẽ báo
            // "sửa/xoá một dòng không tồn tại" (DbUpdateConcurrencyException) — bắt được lỗi này
            // đúng lúc viết test cho luồng thông báo, không phải khi chạy tay.
            _db.LlmOutputs.Add(output);
        }
        // else: đã có sẵn từ FirstOrDefaultAsync → EF đang TRACK nó, chỉ cần sửa field rồi
        // SaveChanges là đủ, không cần Update()/Add() gì thêm.

        output.IsReviewedByHuman = true;
        output.ReviewedBy = reviewerId;
        output.ReviewNotes = JsonSerializer.Serialize(new { verdict = request.Verdict, note });

        _decisions.Log(
            proposal.ProjectId, DecisionTypes.DuplicateReviewed,
            $"Kết luận rà trùng lặp: {VerdictVi(request.Verdict)}",
            VectorEntityType, proposalId.ToString(),
            result: request.Verdict, reason: note,
            decidedBy: reviewerId, decidedByRole: "Phòng QLKH");

        await _db.SaveChangesAsync();

        // Trước đây kết luận chỉ nằm phía Staff — PI không hề biết đề cương mình có bị đối chiếu
        // hay không, kể cả khi kết luận là "cần chỉnh sửa"/"trùng lặp" và họ phải làm gì đó ngay.
        // Cùng lỗi cũ đã sửa ở luồng nộp đề cương: im lặng đúng chỗ người ta cần biết nhất.
        var ketQua = request.Verdict switch
        {
            DuplicateVerdict.NotDuplicate => "không phát hiện trùng lặp đáng kể",
            DuplicateVerdict.NeedsRevision => "cần chỉnh sửa để phân biệt rõ hơn với đề tài đã có",
            DuplicateVerdict.Duplicate => "được đánh giá là trùng lặp",
            _ => request.Verdict
        };
        await _notifier.NotifyAsync(
            proposal.PiUserId,
            "DUPLICATE_CHECK_REVIEWED",
            "Kết quả rà trùng lặp đề cương",
            $"Đề cương \"{proposal.TitleVi}\" đã được Phòng QLKH đối chiếu với kho đề tài: {ketQua}."
                + (string.IsNullOrWhiteSpace(note) ? "" : $" Ghi chú: {note}"),
            // Thiếu actionUrl thì thông báo chỉ là một dòng chữ thoáng qua trong chuông — bấm vào
            // không đi đâu cả, và một khi PI gạt qua thì kết luận biến mất khỏi tầm mắt vĩnh viễn.
            // Đưa thẳng về trang chi tiết đề cương, nơi vừa thêm thẻ kết luận cố định.
            actionUrl: $"/my-proposals/{proposalId}",
            entityType: "Proposal", entityId: proposalId.ToString());

        return await GetAsync(proposalId, reviewerId, roles);
    }

    private static string VerdictVi(string verdict) => verdict switch
    {
        DuplicateVerdict.NotDuplicate => "Không trùng lặp",
        DuplicateVerdict.NeedsRevision => "Cần chỉnh sửa để phân biệt",
        DuplicateVerdict.Duplicate => "Trùng lặp",
        _ => verdict
    };

    private async Task FillReviewStateAsync(DuplicateCheckResponse dto, Guid proposalId)
    {
        var output = await _db.LlmOutputs
            .Where(o => o.EntityType == VectorEntityType && o.EntityId == proposalId.ToString()
                        && o.OutputType == DuplicateOutputType && o.IsActive)
            .Select(o => new
            {
                o.Content, o.GeneratedAt, o.ModelUsed, o.IsReviewedByHuman, o.ReviewNotes,
                ReviewerName = o.ReviewedByUser != null ? o.ReviewedByUser.FullName : null
            })
            .FirstOrDefaultAsync();

        if (output == null) return;

        if (!string.IsNullOrWhiteSpace(output.Content))
        {
            dto.Explanation = output.Content;
            dto.ExplanationGeneratedAt = output.GeneratedAt;
            dto.ExplanationModel = output.ModelUsed;
        }

        if (!output.IsReviewedByHuman || string.IsNullOrWhiteSpace(output.ReviewNotes)) return;

        try
        {
            using var doc = JsonDocument.Parse(output.ReviewNotes);
            if (doc.RootElement.TryGetProperty("verdict", out var v)) dto.Verdict = v.GetString();
            if (doc.RootElement.TryGetProperty("note", out var n)) dto.VerdictNote = n.GetString();
        }
        catch
        {
            // Ghi chú cũ ở dạng chữ thường — hiện nguyên văn còn hơn nuốt mất.
            dto.VerdictNote = output.ReviewNotes;
        }

        dto.ReviewedAt = output.GeneratedAt;
        dto.ReviewedByName = output.ReviewerName;
    }

    // ══════════════════════════════════════════════════════════════════════
    // LẬP CHỈ MỤC
    // ══════════════════════════════════════════════════════════════════════

    public async Task<ReindexEmbeddingsResponse> ReindexAsync(
        Guid? proposalId, int max, CancellationToken ct = default)
    {
        var result = new ReindexEmbeddingsResponse { Model = _gemini.EmbeddingModel };

        if (!_gemini.IsConfigured)
            throw new InvalidOperationException(
                "Chưa cấu hình GeminiAI:ApiKey — không vector hoá được. " +
                "Thêm khoá vào cấu hình rồi chạy lại.");

        var proposals = await _db.Proposals
            .IgnoreQueryFilters()
            .Where(p => (proposalId == null || p.Id == proposalId)
                        && p.SubmittedAt != null)
            .OrderByDescending(p => p.SubmittedAt)
            .Select(p => new ProposalSnapshot
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                PiUserId = p.Project.PiUserId,
                TitleVi = p.TitleVi,
                AbstractVi = p.AbstractVi,
                ResearchObjectives = p.ResearchObjectives,
                ExpectedOutput = p.ExpectedOutput
            })
            .Take(Math.Clamp(max, 1, 500))
            .ToListAsync(ct);

        result.Scanned = proposals.Count;

        foreach (var p in proposals)
        {
            ct.ThrowIfCancellationRequested();

            var content = BuildIndexedContent(p);
            var hash = Sha256(content);

            var row = await _db.SemanticSearchVectors
                .FirstOrDefaultAsync(v => v.EntityType == VectorEntityType && v.EntityId == p.Id.ToString(), ct);

            // Nội dung không đổi và đã có vector cùng model ⇒ bỏ qua. Đây là thứ khiến chạy lại
            // mười lần cũng chỉ tốn quota cho những bản thật sự mới.
            if (row is { Embedding: not null } && row.ContentHash == hash && row.ModelUsed == _gemini.EmbeddingModel)
            {
                result.Unchanged++;
                continue;
            }

            try
            {
                var vector = await _gemini.EmbedAsync(content, ct);

                if (row == null)
                {
                    row = new SemanticSearchVector
                    {
                        EntityType = VectorEntityType,
                        EntityId = p.Id.ToString()
                    };
                    _db.SemanticSearchVectors.Add(row);
                }

                row.ContentSnapshot = Clip(content, 2000);
                row.ContentHash = hash;
                row.Embedding = VectorMath.Serialize(vector);
                row.Dimensions = vector.Length;
                row.ModelUsed = _gemini.EmbeddingModel;
                row.IndexStatus = "INDEXED";
                row.LastIndexedAt = _clock.UtcNow;

                await _db.SaveChangesAsync(ct);
                result.Embedded++;

                // Nghỉ giữa hai lần gọi — cùng lý do với hàng đợi tóm tắt: nã liên tục là ăn 429.
                await Task.Delay(TimeSpan.FromMilliseconds(400), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Một đề cương hỏng không được làm đổ cả mẻ — ghi log rồi đi tiếp.
                result.Failed++;
                _logger.LogWarning(ex, "Không vector hoá được đề cương {ProposalId}", p.Id);
            }
        }

        return result;
    }

    /// <summary>
    /// Nội dung đem đi vector hoá.
    ///
    /// <para>Gộp tên + tóm tắt + mục tiêu + sản phẩm dự kiến. Chỉ lấy mỗi tên thì hai đề tài đặt
    /// tên na ná nhau đã báo động, còn hai đề tài trùng nội dung mà đặt tên khác thì lọt.</para>
    /// </summary>
    private static string BuildIndexedContent(ProposalSnapshot p) =>
        string.Join("\n", new[]
        {
            p.TitleVi,
            p.AbstractVi,
            p.ResearchObjectives,
            p.ExpectedOutput
        }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    // ══════════════════════════════════════════════════════════════════════

    private sealed class ProposalSnapshot
    {
        public Guid Id { get; init; }
        public Guid ProjectId { get; init; }
        public Guid PiUserId { get; init; }
        public string TitleVi { get; init; } = null!;
        public string? AbstractVi { get; init; }
        public string? ResearchObjectives { get; init; }
        public string? ExpectedOutput { get; init; }
    }

    private async Task<ProposalSnapshot> LoadProposalAsync(Guid proposalId) =>
        await _db.Proposals
            .IgnoreQueryFilters()
            .Where(p => p.Id == proposalId)
            .Select(p => new ProposalSnapshot
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                PiUserId = p.Project.PiUserId,
                TitleVi = p.TitleVi,
                AbstractVi = p.AbstractVi,
                ResearchObjectives = p.ResearchObjectives,
                ExpectedOutput = p.ExpectedOutput
            })
            .FirstOrDefaultAsync()
        ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

    /// <summary>Ai xem được — cùng quy tắc với sổ quyết định và màn kinh phí.</summary>
    private async Task AssertCanViewAsync(
        Guid projectId, Guid piUserId, Guid userId, IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleSet.Contains("Admin") || roleSet.Contains("Staff")) return;
        if (piUserId == userId) return;

        var isCouncilMember = await _db.CouncilProjectAssignments
            .Where(a => a.ProjectId == projectId)
            .AnyAsync(a => _db.CouncilMembers
                .Any(m => m.CouncilId == a.CouncilId && m.UserId == userId));
        if (isCouncilMember) return;

        throw new ForbiddenException("Bạn không có quyền xem kết quả rà trùng lặp của đề tài này.");
    }

    private Task<decimal> GetDecimalSettingAsync(string key, string fallback) =>
        _settings.GetDecimalAsync(key, decimal.Parse(fallback, CultureInfo.InvariantCulture));
}
