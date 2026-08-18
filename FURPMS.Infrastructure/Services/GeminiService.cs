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

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["GeminiAI:ApiKey"];
        _model = config["GeminiAI:Model"] ?? "gemini-flash-latest";
        _fallbackModel = config["GeminiAI:FallbackModel"] ?? "gemini-2.5-flash-lite";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

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
            var model = attempt < MaxAttempts ? _model : _fallbackModel;
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
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
                _ => "Kiểm tra lại API key/model."
            };
            throw new InvalidOperationException($"Gemini API lỗi {(int)status}: {hint}{(reason != null ? $" ({reason})" : "")}");
        }

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
