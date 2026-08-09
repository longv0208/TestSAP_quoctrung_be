using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/profile")]
public class AcademicProfilesController : ControllerBase
{
    private readonly IMasterDataRepository _repo;

    public AcademicProfilesController(IMasterDataRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> Get(Guid userId)
    {
        var requesterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
        if (userId != requesterId && !isAdminOrStaff)
            throw new UnauthorizedAccessException("You can only view your own profile.");

        var profile = await _repo.AcademicProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        return Ok(ApiResponse<AcademicProfileResponse?>.Ok(profile == null ? null : AcademicProfileResponse.From(profile)));
    }

    [HttpPut]
    public async Task<IActionResult> Upsert(Guid userId, [FromBody] AcademicProfileRequest request)
    {
        var requesterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
        if (userId != requesterId && !isAdminOrStaff)
            throw new UnauthorizedAccessException("You can only update your own profile.");

        var profile = await _repo.AcademicProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new AcademicProfile { UserId = userId };
            _repo.Add(profile);
        }

        profile.AcademicTitle = request.AcademicTitle;
        profile.ScientificRank = request.ScientificRank;
        profile.DegreeLevel = request.DegreeLevel;
        profile.Specialization = request.Specialization;
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender;
        profile.Hometown = request.Hometown;
        profile.Nationality = request.Nationality ?? "Việt Nam";
        profile.GsPgsYear = request.GsPgsYear;
        profile.GsPgsInstitution = request.GsPgsInstitution;
        profile.IsiScopusCount = request.IsiScopusCount;
        profile.IntlJournalCount = request.IntlJournalCount;
        profile.DomesticJournalCount = request.DomesticJournalCount;
        profile.IntlConferenceCount = request.IntlConferenceCount;
        profile.DomesticConferenceCount = request.DomesticConferenceCount;
        profile.PatentsCount = request.PatentsCount;
        profile.PhdSupervisedCount = request.PhdSupervisedCount;
        profile.MasterSupervisedCount = request.MasterSupervisedCount;
        profile.Institution = request.Institution;
        profile.InstitutionAddress = request.InstitutionAddress;
        profile.SpecializationAreas = request.SpecializationAreas;
        profile.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
        return Ok(ApiResponse<AcademicProfileResponse>.Ok(AcademicProfileResponse.From(profile)));
    }
}

/// <summary>
/// Trả về DTO chứ KHÔNG trả thẳng entity: hồ sơ nay có thêm số tài khoản và CCCD (C3), mà endpoint
/// này Admin/Staff cũng gọi được. Trả entity là lộ nguyên số cho người không phải chính chủ.
/// Số đầy đủ chỉ ra khỏi hệ thống qua **file Word hợp đồng**; xem `ContractIdentityController`
/// cho bản đã che dành cho chính chủ.
/// Tên trường giữ y hệt entity để giao diện không phải sửa gì.
/// </summary>
public class AcademicProfileResponse
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
    public string? Nationality { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static AcademicProfileResponse From(AcademicProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        AcademicTitle = p.AcademicTitle,
        ScientificRank = p.ScientificRank,
        DegreeLevel = p.DegreeLevel,
        Specialization = p.Specialization,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        Hometown = p.Hometown,
        Nationality = p.Nationality,
        GsPgsYear = p.GsPgsYear,
        GsPgsInstitution = p.GsPgsInstitution,
        IsiScopusCount = p.IsiScopusCount,
        IntlJournalCount = p.IntlJournalCount,
        DomesticJournalCount = p.DomesticJournalCount,
        IntlConferenceCount = p.IntlConferenceCount,
        DomesticConferenceCount = p.DomesticConferenceCount,
        PatentsCount = p.PatentsCount,
        PhdSupervisedCount = p.PhdSupervisedCount,
        MasterSupervisedCount = p.MasterSupervisedCount,
        Institution = p.Institution,
        InstitutionAddress = p.InstitutionAddress,
        SpecializationAreas = p.SpecializationAreas,
        TotalInvitations = p.TotalInvitations,
        IsEligiblePi = p.IsEligiblePi,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}

public class AcademicProfileRequest
{
    public string? AcademicTitle { get; set; }
    public string? ScientificRank { get; set; }
    public string? DegreeLevel { get; set; }
    public string? Specialization { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Hometown { get; set; }
    public string? Nationality { get; set; }
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
}
