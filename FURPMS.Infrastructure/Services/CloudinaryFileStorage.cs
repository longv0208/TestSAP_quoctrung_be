using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FURPMS.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace FURPMS.Infrastructure.Services;

/// <summary>
/// Lưu file trên Cloudinary — dùng cho production (Render xoá sạch đĩa mỗi lần redeploy).
///
/// <para><b>Vì sao upload kiểu <c>raw</c>:</b> tài liệu ở đây là .docx/.pdf, không phải ảnh.
/// Cloudinary bắt buộc dùng <c>resource_type=raw</c> cho loại này.</para>
///
/// <para><b>Vì sao vẫn tải qua BE:</b> đã kiểm chứng — URL Cloudinary trả về **tải được mà
/// không cần đăng nhập**, kể cả kiểu <c>authenticated</c> (chữ ký nằm sẵn trong URL và không
/// hết hạn). Đề cương/hợp đồng là tài liệu mật ⇒ URL Cloudinary **chỉ tồn tại phía server**,
/// người dùng luôn đi qua <c>/documents/{id}/download</c> để BE còn kiểm quyền.
/// Lớp bảo vệ thực tế = endpoint có <c>[Authorize]</c> + tên file là GUID không đoán được.</para>
///
/// <para><b>Vì sao KHÔNG lưu URL Cloudinary vào DB:</b> cột <c>Document.StorageUrl</c> đang
/// chứa URL tải của BE và **được trả thẳng cho FE** — nhét URL Cloudinary vào đó là rò rỉ link
/// công khai. Thay vào đó URL được dựng lại từ <c>StorageBlobName</c> mỗi lần cần, nên không
/// phải thêm cột nào.</para>
///
/// <para>Không thêm SDK — Cloudinary chỉ cần REST + chữ ký SHA1, đỡ một dependency.</para>
/// </summary>
public class CloudinaryFileStorage : IFileStorage
{
    private readonly HttpClient _http;
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _folder;

    /// <summary>
    /// Đọc dự phòng từ đĩa cho các file đã upload TRƯỚC khi bật Cloudinary.
    /// Không có cái này thì vừa bật Cloudinary là mọi tài liệu cũ 404 — đúng lỗi gặp lúc test.
    /// </summary>
    private readonly LocalDiskFileStorage _legacyDisk;

    public CloudinaryFileStorage(HttpClient http, IConfiguration config)
    {
        _legacyDisk = new LocalDiskFileStorage(config);
        _http = http;
        _cloudName = config["Cloudinary:CloudName"] ?? "";
        _apiKey = config["Cloudinary:ApiKey"] ?? "";
        _apiSecret = config["Cloudinary:ApiSecret"] ?? "";
        _folder = config["Cloudinary:Folder"] ?? "furpms";
    }

    public static bool IsConfigured(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["Cloudinary:CloudName"])
        && !string.IsNullOrWhiteSpace(config["Cloudinary:ApiKey"])
        && !string.IsNullOrWhiteSpace(config["Cloudinary:ApiSecret"]);

    public string Description => $"Cloudinary (cloud {_cloudName}, thư mục {_folder})";

    /// <summary>Chữ ký Cloudinary: tham số sắp xếp a→z, nối "k=v&…", cộng api_secret, SHA1 hex.</summary>
    private string Sign(SortedDictionary<string, string> parameters)
    {
        var raw = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}")) + _apiSecret;
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    }

    /// <summary>Với raw, public_id GỒM cả phần mở rộng — giữ nguyên blobName cho khớp DB.</summary>
    private string PublicId(string blobName) => $"{_folder}/{blobName}";

    /// <summary>URL đọc, dựng lại được từ blobName (bỏ phần version) — khỏi lưu thêm cột nào.</summary>
    private string DeliveryUrl(string blobName) =>
        $"https://res.cloudinary.com/{_cloudName}/raw/upload/{PublicId(blobName)}";

    public async Task<StoredFile> SaveAsync(
        string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signed = new SortedDictionary<string, string>
        {
            ["public_id"] = PublicId(blobName),
            ["timestamp"] = timestamp,
        };

        using var form = new MultipartFormDataContent();
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;

        var fileContent = new StreamContent(ms);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(fileContent, "\"file\"", $"\"{Path.GetFileName(blobName)}\"");

        // StringContent mặc định kèm "Content-Type: text/plain; charset=utf-8" cho từng field;
        // Cloudinary bỏ qua các field đó ⇒ tưởng là unsigned upload rồi trả 400. Phải gỡ header.
        // Ngoài ra .NET ghi `name=api_key` KHÔNG có dấu nháy; Cloudinary đòi `name="api_key"`.
        void Field(string name, string value)
        {
            var part = new StringContent(value);
            part.Headers.ContentType = null;
            form.Add(part, $"\"{name}\"");
        }

        foreach (var p in signed) Field(p.Key, p.Value);
        Field("api_key", _apiKey);
        Field("signature", Sign(signed));

        HttpResponseMessage resp;
        try
        {
            resp = await _http.PostAsync(
                $"https://api.cloudinary.com/v1_1/{_cloudName}/raw/upload", form, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Mất mạng / DNS hỏng ⇒ KHÔNG chặn người dùng nộp bài. Lưu tạm xuống đĩa;
            // đọc lại vẫn chạy vì OpenAsync đã có nhánh dự phòng đọc từ đĩa.
            ms.Position = 0;
            return await _legacyDisk.SaveAsync(blobName, ms, contentType, ct);
        }

        using (resp)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Không tải được file lên Cloudinary ({(int)resp.StatusCode}): {Trim(body)}");
        }

        // Cố ý KHÔNG trả secure_url của Cloudinary ra ngoài: caller ghi giá trị này vào
        // Document.StorageUrl, mà cột đó lại đi thẳng tới FE.
        return new StoredFile(blobName, DeliveryUrl(blobName));
    }

    public async Task<Stream?> OpenAsync(string blobName, string? storageUrl, CancellationToken ct = default)
    {
        var bytes = await ReadAllBytesAsync(blobName, storageUrl, ct);
        return bytes == null ? null : new MemoryStream(bytes);
    }

    public async Task<byte[]?> ReadAllBytesAsync(string blobName, string? storageUrl, CancellationToken ct = default)
    {
        // storageUrl bỏ qua có chủ ý — nó là URL tải của BE, không phải của Cloudinary.
        try
        {
            using var resp = await _http.GetAsync(DeliveryUrl(blobName), ct);
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException)
        {
            // Mất mạng / DNS hỏng KHÔNG được thành 500. Thử đĩa, không có thì trả null → 404.
        }
        catch (TaskCanceledException)
        {
        }

        // Không lấy được từ Cloudinary ⇒ có thể là file cũ còn nằm trên đĩa.
        return await _legacyDisk.ReadAllBytesAsync(blobName, storageUrl, ct);
    }

    public async Task DeleteAsync(string blobName, string? storageUrl, CancellationToken ct = default)
    {
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_apiKey}:{_apiSecret}"));
        using var req = new HttpRequestMessage(
            HttpMethod.Delete,
            $"https://api.cloudinary.com/v1_1/{_cloudName}/resources/raw/upload" +
            $"?public_ids[]={Uri.EscapeDataString(PublicId(blobName))}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        // Xoá hụt trên Cloudinary không được làm hỏng nghiệp vụ — DB đã đánh dấu IsDeleted.
        try { await _http.SendAsync(req, ct); } catch (HttpRequestException) { }
    }

    private static string Trim(string s) => s.Length > 300 ? s[..300] : s;
}
