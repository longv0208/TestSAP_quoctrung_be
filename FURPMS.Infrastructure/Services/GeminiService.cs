using System.Net;
using System.Text;
using System.Text.Json;
using FURPMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FURPMS.Infrastructure.Services;

// Gọi Google Gemini API để sinh văn bản. Key đọc từ config GeminiAI:ApiKey
// (đặt trong appsettings.Development.json — đã gitignore, KHÔNG commit).
public class GeminiService : IGeminiService
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly string _fallbackModel;
    private readonly string _embeddingModel;

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["GeminiAI:ApiKey"];
        // 2.5 Flash-Lite đã bị Gemini từ chối với một số tài khoản mới dù vẫn xuất hiện trong
        // danh sách model. Dùng tên model ổn định, đã kiểm tra generateContent thực tế, thay vì
        // alias "latest" khó biết đang trỏ vào phiên bản nào khi demo.
        // Mặc định là gemini-embedding-001, KHÔNG phải text-embedding-004 mà tài liệu RP1/RP3/RP7
        // ghi: hỏi ListModels bằng chính khoá của nhóm (26/08) thì text-embedding-004 đã không còn
        // phục vụ embedContent nữa. Tài liệu phải sửa theo cái đang chạy, không phải ngược lại.
        _embeddingModel = config["GeminiAI:EmbeddingModel"] ?? "gemini-embedding-001";
        _model = config["GeminiAI:Model"] ?? "gemini-3.5-flash-lite";
        _fallbackModel = config["GeminiAI:FallbackModel"] ?? "gemini-3.1-flash-lite";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public string EmbeddingModel => _embeddingModel;

    public Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default)
    {
        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };
        return PostAsync(payload, ct);
    }

    public Task<string> GenerateFromInlineDataAsync(byte[] data, string mimeType, string prompt, CancellationToken ct = default)
    {
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(data) } }
                    }
                }
            }
        };
        return PostAsync(payload, ct);
    }

    /// <summary>
    /// Số lần gọi lại khi Gemini báo bận. 503 ("model is currently experiencing high demand") và
    /// 429 là lỗi <b>tạm thời phía Google</b>, không phải sai key hay sai prompt — gọi lại sau vài
    /// giây thường được ngay.
    /// <para>
    /// Trước 17/08 một cú 503 là hỏng hẳn một thao tác: người dùng bấm "AI phân tích" thấy báo đỏ
    /// "Kiểm tra lại API key/model" — vừa sai nguyên nhân vừa khiến cả nhóm tưởng tính năng AI
    /// chết, trong khi bấm lại lần nữa là chạy.
    /// </para>
    /// </summary>
    private const int MaxAttempts = 3;

    private static bool IsTransient(int status) => status is 429 or 500 or 502 or 503 or 504;

    private async Task<string> PostAsync(object payload, CancellationToken ct)
    {
        var body = await SendAsync("generateContent", payload, _model, _fallbackModel, ct);
        return ParseGeneratedText(body);
    }

    /// <summary>
    /// Vòng gọi HTTP + thử lại + fallback model, <b>dùng chung cho mọi endpoint Gemini</b>.
    ///
    /// <para>Tách ra khỏi <c>PostAsync</c> ngày 26/08 khi thêm <c>embedContent</c>. Chép vòng này
    /// lần thứ hai là chép luôn cả phần dễ chép sai: nó đang giữ lời giải cho những lỗi 429/503
    /// gặp thật hồi 17/08 — backoff luỹ thừa kèm jitter, và fallback sang model nhẹ ở lần cuối.</para>
    /// </summary>
    private async Task<string> SendAsync(
        string method, object payload, string primaryModel, string fallbackModel, CancellationToken ct)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Chưa cấu hình GeminiAI:ApiKey. Thêm key vào appsettings.Development.json và chạy BE ở môi trường Development.");

        var json = JsonSerializer.Serialize(payload);

        string body = string.Empty;
        HttpStatusCode status = default;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // Hai lần đầu ưu tiên model chính; lần cuối dùng model nhẹ hơn. Đây là fallback thật,
            // không chỉ đổi thông báo rồi bắt người dùng tự bấm lại trong lúc demo.
            var model = attempt < MaxAttempts ? primaryModel : fallbackModel;
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:{method}";
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            // Dùng header (hỗ trợ cả key cũ "AIza..." lẫn key mới "AQ...."), không nhét key vào URL.
            req.Headers.Add("X-goog-api-key", _apiKey);
            try
            {
                using var resp = await _http.SendAsync(req, ct);
                body = await resp.Content.ReadAsStringAsync(ct);
                status = resp.StatusCode;
            }
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !ct.IsCancellationRequested)
            {
                status = HttpStatusCode.ServiceUnavailable;
                body = "";
            }

            if ((int)status is >= 200 and < 300) break;

            if (attempt >= MaxAttempts || !IsTransient((int)status))
                break;

            // Exponential backoff 2s → 4s, cộng jitter để nhiều request lỗi cùng lúc không
            // thức dậy và nã lại Gemini đúng cùng một thời điểm.
            var seconds = Math.Pow(2, attempt) + Random.Shared.NextDouble();
            await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        }

        if ((int)status is < 200 or >= 300)
        {
            var reason = TryGetErrorMessage(body);
            var hint = (int)status switch
            {
                429 => "Gemini đang quá tải hoặc hết hạn mức. Đã thử lại vài lần — chờ một lát rồi bấm lại.",
                503 => "Gemini đang quá tải. Đã thử lại vài lần — chờ một lát rồi bấm lại.",
                403 => "Key bị từ chối (có thể đã bị Google đánh dấu lộ). Hãy tạo key mới.",
                400 => "Yêu cầu không hợp lệ. Kiểm tra lại tên model.",
                404 => "Model AI đang cấu hình không còn khả dụng. Hãy cập nhật GeminiAI:Model/FallbackModel.",
                _ => "Kiểm tra lại API key/model."
            };
            throw new InvalidOperationException($"Gemini API lỗi {(int)status}: {hint}{(reason != null ? $" ({reason})" : "")}");
        }

        return body;
    }

    private static string ParseGeneratedText(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini không trả về kết quả (có thể nội dung bị chặn). Thử lại.");

        var text = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text?.Trim() ?? string.Empty;
    }

    public async Task<GeminiUsage> GenerateWithUsageAsync(string prompt, CancellationToken ct = default)
    {
        var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var body = await SendAsync("generateContent", payload, _model, _fallbackModel, ct);
        sw.Stop();

        var text = ParseGeneratedText(body);

        // usageMetadata là tuỳ chọn phía Google — thiếu thì để null chứ không đoán ra một con số.
        int? tokensIn = null, tokensOut = null;
        using (var doc = JsonDocument.Parse(body))
        {
            if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
            {
                if (usage.TryGetProperty("promptTokenCount", out var pi)) tokensIn = pi.GetInt32();
                if (usage.TryGetProperty("candidatesTokenCount", out var co)) tokensOut = co.GetInt32();
            }
        }

        return new GeminiUsage(text, tokensIn, tokensOut, (int)sw.ElapsedMilliseconds, _model);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Không thể vector hoá một chuỗi rỗng.");

        var payload = new
        {
            model = $"models/{_embeddingModel}",
            content = new { parts = new[] { new { text } } }
        };

        // Model nhúng không có họ fallback — truyền cùng một tên cho cả hai vị trí.
        var body = await SendAsync("embedContent", payload, _embeddingModel, _embeddingModel, ct);

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("embedding", out var emb) ||
            !emb.TryGetProperty("values", out var values))
            throw new InvalidOperationException("Gemini không trả về vector nhúng.");

        var result = new float[values.GetArrayLength()];
        for (var i = 0; i < result.Length; i++) result[i] = values[i].GetSingle();
        return result;
    }

    private static string? TryGetErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var msg))
            {
                var s = msg.GetString();
                return string.IsNullOrWhiteSpace(s) ? null : (s!.Length > 160 ? s[..160] : s);
            }
        }
        catch { /* body không phải JSON */ }
        return null;
    }
}
