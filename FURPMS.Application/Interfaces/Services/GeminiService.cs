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

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["GeminiAI:ApiKey"];
        _model = config["GeminiAI:Model"] ?? "gemini-flash-latest";
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

    private async Task<string> PostAsync(object payload, CancellationToken ct)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Chưa cấu hình GeminiAI:ApiKey. Thêm key vào appsettings.Development.json và chạy BE ở môi trường Development.");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        // Dùng header (hỗ trợ cả key cũ "AIza..." lẫn key mới "AQ...."), không nhét key vào URL.
        req.Headers.Add("X-goog-api-key", _apiKey);
        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            var reason = TryGetErrorMessage(body);
            var hint = (int)resp.StatusCode switch
            {
                429 => "Hết hạn mức Gemini (quota). Dùng key khác hoặc chờ reset.",
                403 => "Key bị từ chối (có thể đã bị Google đánh dấu lộ). Hãy tạo key mới.",
                400 => "Yêu cầu không hợp lệ. Kiểm tra lại tên model.",
                _ => "Kiểm tra lại API key/model."
            };
            throw new InvalidOperationException($"Gemini API lỗi {(int)resp.StatusCode}: {hint}{(reason != null ? $" ({reason})" : "")}");
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
