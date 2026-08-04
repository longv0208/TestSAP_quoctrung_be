using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FURPMS.Application.Interfaces;

namespace FURPMS.Infrastructure.Services;

/// <summary>
/// Đưa một file bất kỳ cho Gemini đọc, chọn đúng cách theo định dạng.
/// <para>
/// Gemini **KHÔNG nhận .docx** dạng inline (trả 400 <c>Unsupported MIME type</c>) —
/// mà .docx lại là định dạng phổ biến nhất của đề cương. Với .docx/.txt phải bóc
/// text ra rồi gửi như văn bản; chỉ PDF/ảnh mới gửi thẳng bytes.
/// </para>
/// <para>Gom về đây vì cả trích xuất đề cương lẫn đối chiếu form↔file đều cần.</para>
/// </summary>
public static class GeminiFileInput
{
    /// <summary>Định dạng gửi thẳng bytes cho Gemini được.</summary>
    private static readonly HashSet<string> InlineMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/png", "image/jpeg", "image/webp", "image/heic", "image/heif"
    };

    /// <summary>
    /// Gọi Gemini với <paramref name="prompt"/> kèm nội dung file.
    /// Ném <see cref="ArgumentException"/> nếu định dạng không đọc được.
    /// </summary>
    public static Task<string> AskAboutFileAsync(
        IGeminiService gemini,
        byte[] content,
        string fileName,
        string mimeType,
        string prompt,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext == ".pdf" || InlineMimeTypes.Contains(mimeType))
            return gemini.GenerateFromInlineDataAsync(content, NormalizeMime(ext, mimeType), prompt, ct);

        var text = ext switch
        {
            ".docx" => ExtractDocxText(content),
            ".txt" or ".md" => Encoding.UTF8.GetString(content),
            _ => throw new ArgumentException(
                $"Không đọc được định dạng \"{ext}\". Chỉ hỗ trợ PDF, DOCX, TXT.")
        };

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("File không có nội dung văn bản để AI đọc.");

        return gemini.GenerateTextAsync($"{prompt}\n\n--- NỘI DUNG FILE ---\n{text}", ct);
    }

    private static string NormalizeMime(string ext, string mimeType) =>
        ext == ".pdf" ? "application/pdf" : mimeType;

    public static string ExtractDocxText(byte[] content)
    {
        using var ms = new MemoryStream(content);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var para in body.Descendants<Paragraph>())
            sb.AppendLine(para.InnerText);
        return sb.ToString();
    }
}
