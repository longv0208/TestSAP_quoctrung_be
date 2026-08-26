using System.Globalization;
using System.Text;

namespace FURPMS.Application.Common;

/// <summary>
/// Phép toán trên vector nhúng — <b>thuần tính toán, không phụ thuộc gì</b>, nên test được offline.
///
/// <para>Đây là lý do tầng 1 của rà trùng lặp đo được Precision/Recall: cùng một cặp văn bản luôn
/// cho cùng một điểm số. Nếu để mô hình sinh chữ tự chấm "giống bao nhiêu phần trăm" thì mỗi lần
/// chạy ra một số khác, và không bảo vệ được bất kỳ con số metric nào trước hội đồng.</para>
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// Cosine similarity, trả về trong khoảng [-1, 1] (thực tế với văn bản luôn dương).
    ///
    /// <para>Trả 0 khi hai vector khác số chiều: đó là dấu hiệu đổi model mà quên vector hoá lại,
    /// và 0 nghĩa là "không kết luận được", an toàn hơn là ném lỗi giữa lúc chủ nhiệm bấm nộp.</para>
    /// </summary>
    public static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || a.Length != b.Length) return 0d;

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            magA += (double)a[i] * a[i];
            magB += (double)b[i] * b[i];
        }

        if (magA <= 0 || magB <= 0) return 0d;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    /// <summary>
    /// Vector → JSON mảng số. Dùng <see cref="CultureInfo.InvariantCulture"/> để máy đặt ngôn ngữ
    /// tiếng Việt không ghi ra dấu phẩy thập phân rồi đọc lại thành mảng hỏng.
    /// </summary>
    public static string Serialize(float[] vector)
    {
        var sb = new StringBuilder(vector.Length * 12);
        sb.Append('[');
        for (var i = 0; i < vector.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(vector[i].ToString("R", CultureInfo.InvariantCulture));
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>JSON mảng số → vector. Chuỗi hỏng hoặc rỗng trả về mảng rỗng, không ném lỗi.</summary>
    public static float[] Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<float>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
        }
        catch
        {
            // Bản ghi hỏng thì coi như chưa có vector — lần vector hoá sau sẽ ghi đè.
            return Array.Empty<float>();
        }
    }
}
