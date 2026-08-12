using FURPMS.Domain.Entities.Users;

namespace FURPMS.Application.DTOs.Users;

/// <summary>Một dòng lý lịch khoa học trả về cho giao diện (QĐ543 — BM02).</summary>
public class AcademicWorkResponse
{
    public Guid Id { get; set; }
    public string WorkType { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Venue { get; set; }
    public string? Authors { get; set; }
    public string? Role { get; set; }
    public int? Year { get; set; }
    public int? StartYear { get; set; }
    public string? Identifier { get; set; }
    public string? Volume { get; set; }
    public string? Pages { get; set; }
    public string? Status { get; set; }
    public string? Url { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }

    public static AcademicWorkResponse From(AcademicWork w) => new()
    {
        Id = w.Id,
        WorkType = w.WorkType,
        Category = w.Category,
        Title = w.Title,
        Venue = w.Venue,
        Authors = w.Authors,
        Role = w.Role,
        Year = w.Year,
        StartYear = w.StartYear,
        Identifier = w.Identifier,
        Volume = w.Volume,
        Pages = w.Pages,
        Status = w.Status,
        Url = w.Url,
        Note = w.Note,
        SortOrder = w.SortOrder
    };
}

public class AcademicWorkRequest
{
    public string WorkType { get; set; } = WorkTypes.Publication;
    public string Category { get; set; } = WorkCategories.JournalDomestic;
    public string Title { get; set; } = null!;
    public string? Venue { get; set; }
    public string? Authors { get; set; }
    public string? Role { get; set; }
    public int? Year { get; set; }
    public int? StartYear { get; set; }
    public string? Identifier { get; set; }
    public string? Volume { get; set; }
    public string? Pages { get; set; }
    public string? Status { get; set; }
    public string? Url { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }
}
