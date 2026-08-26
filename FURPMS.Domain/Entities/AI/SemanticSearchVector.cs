namespace FURPMS.Domain.Entities.AI;

public class SemanticSearchVector
{
    public int Id { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? ContentSnapshot { get; set; }
    public string ContentHash { get; set; } = null!;
    public string? ElasticsearchDocId { get; set; }
    public DateTime LastIndexedAt { get; set; } = DateTime.UtcNow;
    public string IndexStatus { get; set; } = "PENDING";

    /// <summary>
    /// Vector nhúng, lưu dạng JSON mảng số thực (thêm 26/08).
    ///
    /// <para><b>Vì sao là text chứ không phải pgvector:</b> ảnh <c>postgres:16</c> chuẩn không kèm
    /// extension; <c>CREATE EXTENSION vector</c> mà fail thì <c>Migrate()</c> lúc khởi động làm app
    /// <b>không lên nổi</b> trên bản deploy đang có dữ liệu thật. Kho đề tài cỡ vài trăm bản ghi
    /// nên quét tuần tự trong bộ nhớ vẫn dưới một giây — lợi ích của chỉ mục vector ở quy mô này
    /// bằng không, còn rủi ro thì không.</para>
    ///
    /// <para>1000 đề cương × 768 chiều ≈ 12 MB.</para>
    /// </summary>
    public string? Embedding { get; set; }

    /// <summary>Số chiều của vector — để phát hiện ngay khi đổi model mà quên vector hoá lại.</summary>
    public int? Dimensions { get; set; }

    /// <summary>Model đã sinh ra vector này (vd <c>text-embedding-004</c>).</summary>
    public string? ModelUsed { get; set; }
}
