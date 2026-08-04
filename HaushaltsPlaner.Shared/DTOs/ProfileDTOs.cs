namespace HaushaltsPlaner.Shared.DTOs;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Role { get; set; }
    public string? HouseholdName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UploadProfileImageResponse
{
    public bool Success { get; set; }
    public string? ImageUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
