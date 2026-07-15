namespace FURPMS.Application.DTOs.Councils;

// Gửi thư mời đồng loạt cho các thành viên đã gán. ConfirmDeadline = hạn xác nhận/từ chối.
public class SendInvitationsRequest
{
    public DateTime? ConfirmDeadline { get; set; }
}
