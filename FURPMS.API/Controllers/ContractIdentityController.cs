using System.Security.Claims;
using System.Text.Json;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Users;
using FURPMS.Domain.Entities.Logs;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

/// <summary>
/// C3 — thông tin định danh của Bên B để điền hợp đồng: số tài khoản ngân hàng và CCCD.
/// <para>
/// Căn cứ thu thập: BM05 <b>Điều 7.2</b> — Bên B <em>"ủy quyền cho Trường ĐH FPT khai báo thông tin
/// định danh để cấp chứng thư số"</em> (hợp đồng ký điện tử qua Econtract). Mẫu giấy có sẵn các ô
/// này ở phần BÊN B; trước đây hệ thống không có cột nào nên bản Word xuất ra luôn để dấu chấm lửng.
/// </para>
/// <para>
/// Ba ràng buộc, cả ba đều là quyết định chứ không phải mặc định kỹ thuật:
/// <list type="number">
/// <item><b>Chỉ chính chủ khai</b> — Staff/Admin không gõ hộ, kể cả khi đang lập hợp đồng cho người đó.</item>
/// <item><b>Đọc ra luôn bị che</b> (<c>****1234</c>). Số đầy đủ không đi ra khỏi máy chủ qua API;
/// nó chỉ được đổ thẳng vào file Word lúc xuất hợp đồng.</item>
/// <item><b>Tuỳ chọn</b> — chưa khai thì bản Word chừa trống như bản giấy, bổ sung sau cũng được.
/// KHÔNG chặn việc lập hợp đồng (user chốt 08/08).</item>
/// </list>
/// </para>
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/me/contract-identity")]
public class ContractIdentityController : ControllerBase
{
    private readonly FURPMSDbContext _db;

    public ContractIdentityController(FURPMSDbContext db) => _db = db;

    [ProducesResponseType(typeof(ApiResponse<ContractIdentityResponse>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = CurrentUserId();
        var profile = await _db.AcademicProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        return Ok(ApiResponse<ContractIdentityResponse>.Ok(Map(profile)));
    }

    [ProducesResponseType(typeof(ApiResponse<ContractIdentityResponse>), StatusCodes.Status200OK)]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateContractIdentityRequest request)
    {
        var userId = CurrentUserId();

        var profile = await _db.AcademicProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new AcademicProfile { UserId = userId };
            _db.AcademicProfiles.Add(profile);
        }

        // Ghi lại TRẠNG THÁI cũ (đã che) để đối chiếu khi có tranh chấp — tuyệt đối không ghi số
        // thật vào nhật ký, làm thế là nhân bản dữ liệu nhạy cảm sang một bảng ai cũng đọc được.
        var before = Map(profile);

        var changed = new List<string>();
        if (request.BankAccountNumber != null)
        {
            var value = NormalizeDigits(request.BankAccountNumber);
            if (value != null && !value.All(char.IsDigit))
                throw new ArgumentException("Số tài khoản chỉ gồm chữ số.");
            if (profile.BankAccountNumber != value) changed.Add("bankAccountNumber");
            profile.BankAccountNumber = value;
        }
        if (request.BankName != null)
        {
            var value = NormalizeText(request.BankName);
            if (profile.BankName != value) changed.Add("bankName");
            profile.BankName = value;
        }
        if (request.NationalId != null)
        {
            var value = NormalizeDigits(request.NationalId);
            // CCCD 12 số, CMND cũ 9 số — chấp nhận cả hai vì hồ sơ cũ vẫn dùng CMND.
            if (value != null && !(value.All(char.IsDigit) && (value.Length == 9 || value.Length == 12)))
                throw new ArgumentException("Số CCCD phải là 12 chữ số (hoặc 9 chữ số nếu là CMND cũ).");
            if (profile.NationalId != value) changed.Add("nationalId");
            profile.NationalId = value;
        }
        if (request.NationalIdIssuedDate != null)
        {
            if (request.NationalIdIssuedDate > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new ArgumentException("Ngày cấp không thể ở tương lai.");
            if (profile.NationalIdIssuedDate != request.NationalIdIssuedDate) changed.Add("nationalIdIssuedDate");
            profile.NationalIdIssuedDate = request.NationalIdIssuedDate;
        }
        if (request.NationalIdIssuedPlace != null)
        {
            var value = NormalizeText(request.NationalIdIssuedPlace);
            if (profile.NationalIdIssuedPlace != value) changed.Add("nationalIdIssuedPlace");
            profile.NationalIdIssuedPlace = value;
        }

        profile.UpdatedAt = DateTime.UtcNow;

        if (changed.Count > 0)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "UPDATE_CONTRACT_IDENTITY",
                EntityType = nameof(AcademicProfile),
                EntityId = userId.ToString(),
                OldValues = JsonSerializer.Serialize(before),
                NewValues = JsonSerializer.Serialize(new { changedFields = changed }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
        }

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<ContractIdentityResponse>.Ok(Map(profile)));
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Chữ tự do: chỉ cắt khoảng trắng thừa. Gửi chuỗi rỗng = xoá.</summary>
    private static string? NormalizeText(string raw)
    {
        var trimmed = raw.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    /// <summary>
    /// Số tài khoản / CCCD: bỏ hết khoảng trắng vì người dùng hay gõ theo nhóm ("1900 1234 5678").
    /// KHÔNG dùng cho tên ngân hàng hay nơi cấp — làm thế sẽ dính hết chữ vào nhau.
    /// </summary>
    private static string? NormalizeDigits(string raw)
    {
        var trimmed = raw.Replace(" ", "").Replace(".", "").Replace("-", "").Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    /// <summary>Giữ lại 4 ký tự cuối là đủ để chính chủ nhận ra số của mình mà không lộ cả số.</summary>
    private static string? Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Length <= 4 ? new string('*', value.Length) : new string('*', 4) + value[^4..];
    }

    private static ContractIdentityResponse Map(AcademicProfile? p)
    {
        var dto = new ContractIdentityResponse
        {
            BankAccountNumberMasked = Mask(p?.BankAccountNumber),
            BankName = p?.BankName,
            NationalIdMasked = Mask(p?.NationalId),
            NationalIdIssuedDate = p?.NationalIdIssuedDate,
            NationalIdIssuedPlace = p?.NationalIdIssuedPlace,
            HasBankAccount = !string.IsNullOrWhiteSpace(p?.BankAccountNumber),
            HasNationalId = !string.IsNullOrWhiteSpace(p?.NationalId)
        };

        // Nhắc nhở, không phải điều kiện chặn.
        if (string.IsNullOrWhiteSpace(p?.BankAccountNumber)) dto.MissingForContract.Add("Số tài khoản ngân hàng");
        if (string.IsNullOrWhiteSpace(p?.BankName)) dto.MissingForContract.Add("Tên ngân hàng");
        if (string.IsNullOrWhiteSpace(p?.NationalId)) dto.MissingForContract.Add("Số CCCD");
        if (p?.NationalIdIssuedDate == null) dto.MissingForContract.Add("Ngày cấp CCCD");
        if (string.IsNullOrWhiteSpace(p?.NationalIdIssuedPlace)) dto.MissingForContract.Add("Nơi cấp CCCD");
        return dto;
    }
}
