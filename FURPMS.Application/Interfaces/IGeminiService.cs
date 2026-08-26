namespace FURPMS.Application.Interfaces;

/// <summary>Số token và thời gian của một lần gọi Gemini — để ghi vào <c>llm_outputs</c>.</summary>
public record GeminiUsage(string Text, int? TokensInput, int? TokensOutput, int LatencyMs, string ModelUsed);

public interface IGeminiService
{
    bool IsConfigured { get; }
    Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default);

    /// <summary>
    /// Như <see cref="GenerateTextAsync"/> nhưng trả kèm số token và độ trễ.
    ///
    /// <para>Ba cột <c>TokensInput</c>/<c>TokensOutput</c>/<c>LatencyMs</c> có trong bảng
    /// <c>llm_outputs</c> từ đầu nhưng <b>chưa luồng nào ghi</b>. Không ghi thì không trả lời được
    /// câu "nhóm có quản lý chi phí AI không" — mà đó là câu hội đồng chắc chắn hỏi.</para>
    /// </summary>
    Task<GeminiUsage> GenerateWithUsageAsync(string prompt, CancellationToken ct = default);

    // Gửi kèm file (PDF/ảnh...) dạng inline base64 cho Gemini multimodal đọc.
    Task<string> GenerateFromInlineDataAsync(byte[] data, string mimeType, string prompt, CancellationToken ct = default);

    /// <summary>
    /// Vector hoá một đoạn văn bản bằng model nhúng (mặc định <c>gemini-embedding-001</c>).
    ///
    /// <para>Đây là tầng 1 của rà trùng lặp: <b>tất định</b> nên đo được Precision/Recall, và rẻ hơn
    /// gọi mô hình sinh chữ vài bậc.</para>
    /// </summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>Tên model nhúng đang dùng — ghi kèm vector để biết vector nào sinh bởi model nào.</summary>
    string EmbeddingModel { get; }
}
