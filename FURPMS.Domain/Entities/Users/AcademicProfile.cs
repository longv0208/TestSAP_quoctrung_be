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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
