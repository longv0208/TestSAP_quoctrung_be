namespace FURPMS.Application.Interfaces;

public interface IGeminiService
{
    bool IsConfigured { get; }
    Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default);
    // Gửi kèm file (PDF/ảnh...) dạng inline base64 cho Gemini multimodal đọc.
    Task<string> GenerateFromInlineDataAsync(byte[] data, string mimeType, string prompt, CancellationToken ct = default);
}
