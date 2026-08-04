using FURPMS.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace FURPMS.Infrastructure.Services;

/// <summary>
/// Lưu file trên đĩa của máy chạy BE — hành vi cũ, dùng khi dev ở máy cá nhân.
/// <para>
/// ⚠️ File nằm THEO THƯ MỤC CHẠY BE. Đổi sang clone/repo khác mà không chép
/// <c>App_Data</c> sang thì mọi tài liệu cũ đều 404 (DB vẫn trỏ tới chúng).
/// </para>
/// <para>⚠️ KHÔNG dùng được trên Render/PaaS — filesystem tạm, redeploy là mất sạch.</para>
/// </summary>
public class LocalDiskFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalDiskFileStorage(IConfiguration config)
    {
        _root = config["DocumentStorage:RootPath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads");
    }

    public string Description => $"đĩa local ({_root})";

    private string FullPath(string blobName) =>
        Path.Combine(_root, blobName.Replace('/', Path.DirectorySeparatorChar));

    public async Task<StoredFile> SaveAsync(string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var path = FullPath(blobName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);

        return new StoredFile(blobName, $"/uploads/{blobName}");
    }

    public Task<Stream?> OpenAsync(string blobName, string? storageUrl, CancellationToken ct = default)
    {
        var path = FullPath(blobName);
        Stream? s = File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read) : null;
        return Task.FromResult(s);
    }

    public async Task<byte[]?> ReadAllBytesAsync(string blobName, string? storageUrl, CancellationToken ct = default)
    {
        var path = FullPath(blobName);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
    }

    public Task DeleteAsync(string blobName, string? storageUrl, CancellationToken ct = default)
    {
        var path = FullPath(blobName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
