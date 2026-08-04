namespace FURPMS.Application.Interfaces.Services;

/// <summary>Kết quả lưu file: tên blob để ghi vào DB + URL nội bộ (nếu chỗ lưu có URL).</summary>
public record StoredFile(string BlobName, string Url);

/// <summary>
/// Nơi cất file đính kèm. Tách riêng vì đĩa local **không dùng được trên production**:
/// Render (và phần lớn PaaS) có filesystem TẠM — mỗi lần redeploy/restart là mất sạch
/// file người dùng đã nộp.
/// <para>
/// ⚠️ URL do chỗ lưu ngoài trả về (kể cả Cloudinary kiểu <c>authenticated</c>) **tải được
/// mà không cần đăng nhập** — đã kiểm chứng. Vì vậy URL đó chỉ giữ ở phía server;
/// người dùng luôn tải qua endpoint <c>/documents/{id}/download</c> của BE để còn
/// kiểm quyền. KHÔNG trả URL chỗ lưu ngoài về cho FE.
/// </para>
/// </summary>
public interface IFileStorage
{
    /// <summary>Mô tả ngắn chỗ đang lưu — để log lúc khởi động, đỡ tưởng nhầm môi trường.</summary>
    string Description { get; }

    Task<StoredFile> SaveAsync(string blobName, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Mở file để đọc. <c>null</c> nếu không còn. <paramref name="storageUrl"/> lấy từ <c>Document.StorageUrl</c>.</summary>
    Task<Stream?> OpenAsync(string blobName, string? storageUrl, CancellationToken ct = default);

    Task<byte[]?> ReadAllBytesAsync(string blobName, string? storageUrl, CancellationToken ct = default);

    Task DeleteAsync(string blobName, string? storageUrl, CancellationToken ct = default);
}
