namespace HaushaltsPlaner.Shared.DTOs;

public class DashboardStatsDto
{
    public int TodoListsCount { get; set; }
    public int OpenTodoItemsCount { get; set; }
    public int UpcomingEventsCount { get; set; }
    public int TodayEventsCount { get; set; }
    public int PlannedMealsCount { get; set; }
    public int UnreadMessagesCount { get; set; }
    public int LocationsCount { get; set; }
    public int PhotosCount { get; set; }
    public int VideosCount { get; set; }
    public int ContactsCount { get; set; }
    public int FamilyMembersCount { get; set; }
    public string? HouseholdBackgroundImageUrl { get; set; }
}
