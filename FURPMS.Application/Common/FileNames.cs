using System.Text;

namespace FURPMS.Application.Common;

/// <summary>
/// Chuẩn hoá tên file người dùng nộp lên, để chỗ nào hiện tên file cũng ra đúng cái tên PI thấy
/// trên máy mình.
///
/// <para><b>Lỗi thật đã gặp (18/08):</b> màn xem tài liệu của hội đồng hiện
/// <c>02%20%C4%90e%CC%82%CC%80%20cu%CC%9Bo%CC%9Bng%20nghie%CC%82n%20cu%CC%9B%CC%81u…</c> thay vì
/// <i>02 Đề cương nghiên cứu…</i> — và bản tóm tắt AI cũng ghi lại nguyên cái tên đó. Không phải
/// FE hiện sai: DB lưu đúng chuỗi ấy, vì <b>file trên máy người nộp vốn đã mang tên bị mã hoá
/// URL</b> (tải về từ một đường dẫn không kèm <c>Content-Disposition</c> nên trình duyệt lấy
/// nguyên đoạn path đã escape làm tên file). BE chép <c>IFormFile.FileName</c> vào DB y nguyên.
/// </para>
///
/// <para>Hai phép sửa, cả hai đều <b>không</b> đổi nội dung tên file hợp lệ:</para>
/// <list type="number">
///   <item>Giải mã <c>%XX</c> — chỉ khi tên KHÔNG chứa khoảng trắng (tên đã mã hoá URL thì khoảng
///   trắng phải là <c>%20</c>), nên tên thật kiểu <c>Giải ngân 50% đợt 1.pdf</c> không bị đụng.</item>
///   <item>Gom dấu tiếng Việt về dạng dựng sẵn (NFC). File từ máy macOS lưu dạng tách rời (NFD):
///   nhìn thì giống nhau nhưng so chuỗi, tìm kiếm, và cả <c>Content-Disposition</c> đều lệch.</item>
/// </list>
///
/// <para>Kèm bỏ đường dẫn thư mục (vài trình duyệt gửi cả <c>C:\Users\…\file.docx</c>) và ký tự
/// điều khiển — thứ có thể bẻ header khi tải về.</para>
/// </summary>
public static class FileNames
{
    /// <summary>
    /// Trả về tên file đã chuẩn hoá. Chuỗi rỗng/null giữ nguyên là rỗng — phần kiểm tra đuôi file
    /// ở tầng trên tự báo lỗi, hàm này không tự đặt tên thay người dùng.
    /// </summary>
    public static string Normalize(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;

        var name = fileName.Trim();

        // Bỏ đường dẫn: chỉ giữ phần sau dấu / hoặc \ cuối cùng.
        var cut = name.LastIndexOfAny(new[] { '/', '\\' });
        if (cut >= 0) name = name[(cut + 1)..];

        name = TryDecodePercentEncoding(name);

        // NFC: "ề" dạng tách rời (e + ◌̂ + ◌̀) gộp lại thành một ký tự.
        try { name = name.Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { /* chuỗi có surrogate hỏng — giữ nguyên còn hơn ném lỗi lúc nộp bài */ }

        // Ký tự điều khiển (xuống dòng, tab…) lọt vào tên file sẽ bẻ header Content-Disposition.
        var clean = new StringBuilder(name.Length);
        foreach (var c in name)
            if (!char.IsControl(c)) clean.Append(c);

        return clean.ToString().Trim();
    }

    /// <summary>
    /// Giải mã <c>%XX</c> — nhưng chỉ khi gần như chắc chắn tên đã bị mã hoá URL.
    ///
    /// <para>Điều kiện: có ít nhất một cụm <c>%XX</c> hợp lệ, <b>và</b> tên không chứa khoảng
    /// trắng. Tên đã mã hoá thì khoảng trắng nằm ở dạng <c>%20</c>, nên tên thật có khoảng trắng
    /// (kiểu <c>Báo cáo 50% tiến độ.pdf</c>) không bao giờ rơi vào nhánh này.</para>
    ///
    /// <para>Giải mã xong mà lòi ra <c>/</c>, <c>\</c> hay ký tự điều khiển thì bỏ kết quả, giữ
    /// tên gốc — không để chuỗi mã hoá biến thành đường dẫn.</para>
    /// </summary>
    private static string TryDecodePercentEncoding(string name)
    {
        if (name.Any(char.IsWhiteSpace) || !HasPercentEscape(name)) return name;

        string decoded;
        try { decoded = Uri.UnescapeDataString(name); }
        catch (UriFormatException) { return name; }

        if (decoded == name) return name;
        if (decoded.Any(c => c is '/' or '\\' || char.IsControl(c))) return name;
        return decoded;
    }

    private static bool HasPercentEscape(string value)
    {
        for (var i = 0; i + 2 < value.Length; i++)
            if (value[i] == '%' && IsHex(value[i + 1]) && IsHex(value[i + 2]))
                return true;
        return false;
    }

    private static bool IsHex(char c) =>
        char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}
