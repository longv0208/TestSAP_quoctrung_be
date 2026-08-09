namespace FURPMS.Domain.Entities.Users;

public class AcademicProfile
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? AcademicTitle { get; set; }
    public string? ScientificRank { get; set; }
    public string? DegreeLevel { get; set; }
    public string? Specialization { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Hometown { get; set; }
    public string? Nationality { get; set; } = "Việt Nam";
    public int? GsPgsYear { get; set; }
    public string? GsPgsInstitution { get; set; }
    public int IsiScopusCount { get; set; }
    public int IntlJournalCount { get; set; }
    public int DomesticJournalCount { get; set; }
    public int IntlConferenceCount { get; set; }
    public int DomesticConferenceCount { get; set; }
    public int PatentsCount { get; set; }
    public int PhdSupervisedCount { get; set; }
    public int MasterSupervisedCount { get; set; }
    public string? Institution { get; set; }
    public string? InstitutionAddress { get; set; }
    public string? SpecializationAreas { get; set; }
    public int TotalInvitations { get; set; }
    public bool IsEligiblePi { get; set; }

    // ── Thông tin định danh của Bên B để lập hợp đồng (C3) ───────────────────
    // BM05 phần "BÊN B" có các ô: số tài khoản + ngân hàng, số CCCD + ngày cấp + nơi cấp.
    // Căn cứ thu thập: BM05 **Điều 7.2** — Bên B *"ủy quyền cho Trường ĐH FPT khai báo thông tin
    // định danh để cấp chứng thư số"* (ký điện tử Econtract).
    //
    // TUỲ CHỌN (user chốt 08/08): chưa khai thì bản Word chừa trống như bản giấy, bổ sung sau cũng
    // được — KHÔNG chặn việc lập hợp đồng. Chỉ **chính chủ** được khai, Staff không gõ hộ.
    // Hiển thị luôn che (`****1234`); bản đầy đủ chỉ đổ vào file Word lúc xuất.
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? NationalId { get; set; }
    public DateOnly? NationalIdIssuedDate { get; set; }
    public string? NationalIdIssuedPlace { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
