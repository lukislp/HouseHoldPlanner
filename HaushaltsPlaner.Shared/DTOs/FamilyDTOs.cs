namespace HaushaltsPlaner.Shared.DTOs;

public class FamilyMemberDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class HouseholdInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public List<FamilyMemberDto> Members { get; set; } = new();

    // Statistics
    public int UpcomingEventsCount { get; set; }
    public int PlannedMealsCount { get; set; }
    public int OpenTodoItemsCount { get; set; }
    public int UnreadMessagesCount { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class UpdateMemberRoleRequest
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}
