namespace HaushaltsPlaner.Shared.Models;

public class Photo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public int HouseholdId { get; set; }
    public int UploadedByUserId { get; set; }

    // Navigation Properties
    public Household Household { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}

public class Video
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int? DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public int HouseholdId { get; set; }
    public int UploadedByUserId { get; set; }

    // Navigation Properties
    public Household Household { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}
