namespace FURPMS.Application.DTOs.Users;

/// <summary>
/// Thông tin định danh Bên B dùng để điền hợp đồng (C3) — trả về ở dạng **ĐÃ CHE**.
/// Số đầy đủ không bao giờ đi ra khỏi máy chủ qua API; nó chỉ được đổ thẳng vào file Word
/// lúc xuất hợp đồng, đúng chỗ mà bản giấy vốn để trống.
/// </summary>
public class ContractIdentityResponse
{
    /// <summary>Vd <c>****1234</c>. <c>null</c> khi chưa khai.</summary>
    public string? BankAccountNumberMasked { get; set; }
    /// <summary>Tên ngân hàng không phải bí mật nên hiện đủ.</summary>
    public string? BankName { get; set; }
    public string? NationalIdMasked { get; set; }
    public DateOnly? NationalIdIssuedDate { get; set; }
    public string? NationalIdIssuedPlace { get; set; }

    /// <summary>Để giao diện biết nên hiện "Thêm" hay "Cập nhật" mà không cần đoán từ chuỗi che.</summary>
    public bool HasBankAccount { get; set; }
    public bool HasNationalId { get; set; }

    /// <summary>
    /// Còn thiếu gì so với mẫu BM05 — hiện thành nhắc nhở nhẹ, KHÔNG chặn lập hợp đồng.
    /// </summary>
    public List<string> MissingForContract { get; set; } = new();
}

/// <summary>
/// Chỉ chính chủ được gửi — Staff/Admin không khai hộ. Mọi trường đều tuỳ chọn: gửi chuỗi rỗng
/// để xoá, bỏ trống (null) để giữ nguyên giá trị cũ.
/// </summary>
public class UpdateContractIdentityRequest
{
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? NationalId { get; set; }
    public DateOnly? NationalIdIssuedDate { get; set; }
    public string? NationalIdIssuedPlace { get; set; }
}
