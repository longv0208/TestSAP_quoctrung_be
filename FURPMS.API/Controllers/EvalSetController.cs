using System.Text.Json;
using System.Text.Json.Nodes;
using FURPMS.Application.Common;
using FURPMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

/// <summary>
/// Vector hoá <b>bộ gán nhãn</b> để đo chất lượng rà trùng lặp.
///
/// <para>Tách khỏi luồng nghiệp vụ vì đây là công cụ đo, chạy một lần rồi thôi: nó đọc
/// <c>FURPMS.Tests/Ai/duplicate-eval-set.json</c>, gọi model nhúng cho từng văn bản, rồi ghi vector
/// ngược vào chính file đó. Từ lúc ấy <c>DuplicateThresholdEvaluationTests</c> quét ngưỡng
/// <b>hoàn toàn offline</b> — số liệu metric không phụ thuộc mạng và không đổi giữa hai lần chạy.</para>
///
/// <para>⚠️ Chỉ bật ngoài Production. Đây là công cụ nội bộ, ghi file trong mã nguồn.</para>
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class EvalSetController : ControllerBase
{
    private readonly IGeminiService _gemini;
    private readonly IWebHostEnvironment _env;

    public EvalSetController(IGeminiService gemini, IWebHostEnvironment env)
    {
        _gemini = gemini;
        _env = env;
    }

    public class EmbedEvalSetResponse
    {
        public int Texts { get; set; }
        public int Embedded { get; set; }
        public int Skipped { get; set; }
        public string? Model { get; set; }
        public string? Path { get; set; }
    }

    [HttpPost("embed-eval-set")]
    public async Task<IActionResult> EmbedEvalSet([FromQuery] bool force = false, CancellationToken ct = default)
    {
        if (_env.IsProduction())
            throw new ForbiddenException("Công cụ đo chỉ chạy ngoài môi trường Production.");

        if (!_gemini.IsConfigured)
            throw new InvalidOperationException("Chưa cấu hình GeminiAI:ApiKey.");

        var path = FindEvalSet()
            ?? throw new FileNotFoundException(
                "Không tìm thấy duplicate-eval-set.json. Chạy API từ thư mục gốc mã nguồn.");

        var root = JsonNode.Parse(await System.IO.File.ReadAllTextAsync(path, ct))!.AsObject();
        var texts = root["texts"]!.AsArray();
        var vectors = root["vectors"]?.AsObject() ?? new JsonObject();

        var result = new EmbedEvalSetResponse
        {
            Texts = texts.Count,
            Model = _gemini.EmbeddingModel,
            Path = path
        };

        foreach (var node in texts)
        {
            ct.ThrowIfCancellationRequested();
            var obj = node!.AsObject();
            var id = obj["id"]!.GetValue<string>();

            if (!force && vectors.ContainsKey(id))
            {
                result.Skipped++;
                continue;
            }

            // Ghép đúng như luồng thật ghép nội dung đem vector hoá — đo trên thứ khác với thứ
            // chạy thật thì con số metric không nói lên điều gì về hệ thống.
            var content = string.Join("\n", new[]
            {
                obj["title"]?.GetValue<string>(),
                obj["abstract"]?.GetValue<string>(),
                obj["objectives"]?.GetValue<string>()
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var vector = await _gemini.EmbedAsync(content, ct);
            vectors[id] = new JsonArray(vector.Select(v => JsonValue.Create(v)).ToArray<JsonNode?>());
            result.Embedded++;

            await Task.Delay(TimeSpan.FromMilliseconds(400), ct);
        }

        root["vectors"] = vectors;
        root["embeddingModel"] = _gemini.EmbeddingModel;
        root["embeddedAt"] = DateTime.UtcNow.ToString("O");

        await System.IO.File.WriteAllTextAsync(
            path,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            ct);

        return Ok(ApiResponse<EmbedEvalSetResponse>.Ok(result));
    }

    /// <summary>Đi ngược lên từ thư mục chạy để tìm file trong mã nguồn.</summary>
    private static string? FindEvalSet()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "FURPMS.Tests", "Ai", "duplicate-eval-set.json");
            if (System.IO.File.Exists(candidate)) return candidate;
        }
        return null;
    }
}
