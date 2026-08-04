using Microsoft.EntityFrameworkCore;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Server.Services;

public class FamilyService
{
    private readonly AppDbContext _context;

    public FamilyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HouseholdInfoDto?> GetHouseholdInfoAsync(int householdId, int currentUserId)
    {
        var household = await _context.Households
       .Include(h => h.Members)
                  .FirstOrDefaultAsync(h => h.Id == householdId);

        if (household == null) return null;

        var members = household.Members.Select(m => new FamilyMemberDto
        {
            Id = m.Id,
            FullName = m.FullName,
            Username = m.Username,
            Email = m.Email,
            ProfileImageUrl = m.ProfileImageUrl,
            Role = m.Role,
            JoinedAt = m.CreatedAt,
            IsCurrentUser = m.Id == currentUserId
        })
           .OrderByDescending(m => m.IsCurrentUser)
           .ThenBy(m => m.FullName)
              .ToList();

        // Get statistics
        var now = DateTime.Now;
        var weekStart = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
        var weekEnd = weekStart.AddDays(6);

        var upcomingEventsCount = await _context.CalendarEvents
      .Where(e => e.HouseholdId == householdId &&
              e.StartDate >= now &&
      e.StartDate <= weekEnd)
   .CountAsync();

        var plannedMealsCount = await _context.MealPlans
              .Where(m => m.HouseholdId == householdId &&
       m.Date >= now.Date)
    .CountAsync();

        var openTodoItemsCount = await _context.TodoItems
       .Where(t => t.TodoList.HouseholdId == householdId &&
       !t.IsCompleted)
     .CountAsync();

        var unreadMessagesCount = await _context.Messages
            .Where(m => m.HouseholdId == householdId &&
     !m.IsRead &&
       (m.RecipientUserId == currentUserId || m.RecipientUserId == null))
       .CountAsync();

        return new HouseholdInfoDto
        {
            Id = household.Id,
            Name = household.Name,
            CreatedAt = household.CreatedAt,
            MemberCount = members.Count,
            Members = members,
            UpcomingEventsCount = upcomingEventsCount,
            PlannedMealsCount = plannedMealsCount,
            OpenTodoItemsCount = openTodoItemsCount,
            UnreadMessagesCount = unreadMessagesCount
        };
    }

    public async Task<bool> RemoveMemberAsync(int userId, int householdId, int currentUserId)
    {
        // Prevent self-removal
        if (userId == currentUserId) return false;

        var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Id == userId && u.HouseholdId == householdId);

        if (user == null) return false;

        // Remove user from household (set to null or delete based on requirements)
        user.HouseholdId = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(UpdateMemberRoleRequest request, int householdId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.HouseholdId == householdId);

        if (user == null) return false;

        user.Role = request.Role;
        await _context.SaveChangesAsync();
        return true;
    }
}
