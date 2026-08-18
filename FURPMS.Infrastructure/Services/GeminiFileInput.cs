using System.Text;
using System.Text.RegularExpressions;
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

        text = CleanText(text);
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("File không có nội dung văn bản để AI đọc.");

        return gemini.GenerateTextAsync($"{prompt}\n\n--- NỘI DUNG FILE ---\n{text}", ct);
    }

    /// <summary>
    /// Bỏ ký tự điều khiển/khoảng trắng rác trước khi gửi model. Giới hạn 120.000 ký tự để một
    /// file Word chứa lịch sử sửa/khối lặp không làm bùng token và kéo cả request vào 429.
    /// </summary>
    public static string CleanText(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var clean = new string(raw.Where(c => c is '\n' or '\r' or '\t' || !char.IsControl(c)).ToArray());
        var lines = clean.Replace("\r", "").Split('\n')
            .Select(line => Regex.Replace(line, @"[ \t]+", " ").Trim())
            .Where(line => line.Length > 0);
        clean = string.Join('\n', lines);
        return clean.Length <= 120_000 ? clean : clean[..120_000];
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
        return CleanText(sb.ToString());
    }
}
