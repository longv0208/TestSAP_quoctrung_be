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
        return Ok(ApiResponse<AcademicProfile?>.Ok(profile));
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
        return Ok(ApiResponse<AcademicProfile>.Ok(profile));
    }
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
