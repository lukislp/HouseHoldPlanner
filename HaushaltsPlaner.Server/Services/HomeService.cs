using Microsoft.EntityFrameworkCore;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Server.Services;

public class HomeService
{
    private readonly AppDbContext _context;

    public HomeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int householdId, int userId)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var weekStart = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
        var weekEnd = weekStart.AddDays(6);

        // Household background image
        var household = await _context.Households
            .FirstOrDefaultAsync(h => h.Id == householdId);

        // Todo Lists and Items
        var todoLists = await _context.TodoLists
                 .Where(tl => tl.HouseholdId == householdId)
            .CountAsync();

        var openTodoItems = await _context.TodoItems
          .Where(t => t.TodoList.HouseholdId == householdId &&
            !t.IsCompleted &&
           (!t.DueDate.HasValue || t.DueDate.Value >= now))
       .CountAsync();

        // Calendar Events - Upcoming this week
        var upcomingEvents = await _context.CalendarEvents
        .Where(e => e.HouseholdId == householdId &&
    e.StartDate >= now &&
    e.StartDate <= weekEnd)
 .CountAsync();

        // Calendar Events - Today
        var todayEvents = await _context.CalendarEvents
     .Where(e => e.HouseholdId == householdId &&
      e.StartDate.Date == today &&
       (e.AssignedToUserId == userId || e.AssignedToUserId == null))
         .CountAsync();

        // Meal Plans
        var plannedMeals = await _context.MealPlans
          .Where(m => m.HouseholdId == householdId &&
      m.Date >= now.Date &&
    m.Date <= weekEnd.Date)
       .CountAsync();

        // Messages
        var unreadMessages = await _context.Messages
            .Where(m => m.HouseholdId == householdId &&
                !m.IsRead &&
           (m.RecipientUserId == userId || m.RecipientUserId == null))
      .CountAsync();

        // TODO: Locations table doesn't exist yet
        var locations = 0;

        // Media (Gallery)
        var photos = await _context.Photos
         .Where(p => p.HouseholdId == householdId)
          .CountAsync();

        var videos = await _context.Videos
.Where(v => v.HouseholdId == householdId)
         .CountAsync();

        // TODO: Contacts table doesn't exist yet
        var contacts = 0;

        // Family Members
        var familyMembers = await _context.Users
      .Where(u => u.HouseholdId == householdId)
            .CountAsync();

        return new DashboardStatsDto
        {
            TodoListsCount = todoLists,
            OpenTodoItemsCount = openTodoItems,
            UpcomingEventsCount = upcomingEvents,
            TodayEventsCount = todayEvents,
            PlannedMealsCount = plannedMeals,
            UnreadMessagesCount = unreadMessages,
            LocationsCount = locations,
            PhotosCount = photos,
            VideosCount = videos,
            ContactsCount = contacts,
            FamilyMembersCount = familyMembers,
            HouseholdBackgroundImageUrl = household?.BackgroundImageUrl
        };
    }

    public async Task<UploadImageResponse> UploadBackgroundImageAsync(int householdId, IFormFile file)
    {
        try
        {
            // Validate file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return new UploadImageResponse
                {
                    Success = false,
                    Message = "Only image files are allowed (jpg, jpeg, png, gif, webp)"
                };
            }

            if (file.Length > 10 * 1024 * 1024) // 10 MB
            {
                return new UploadImageResponse
                {
                    Success = false,
                    Message = "File is too large (max. 10 MB)"
                };
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine("wwwroot", "uploads", "backgrounds");
            Directory.CreateDirectory(uploadsPath);

            // Delete old background if exists
            var household = await _context.Households.FindAsync(householdId);
            if (household != null && !string.IsNullOrEmpty(household.BackgroundImageUrl))
            {
                var oldFilePath = Path.Combine("wwwroot", household.BackgroundImageUrl.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            // Generate unique filename
            var fileName = $"household_{householdId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update database
            var imageUrl = $"/uploads/backgrounds/{fileName}";
            if (household != null)
            {
                household.BackgroundImageUrl = imageUrl;
                await _context.SaveChangesAsync();
            }

            return new UploadImageResponse
            {
                Success = true,
                ImageUrl = imageUrl,
                Message = "Background image uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            return new UploadImageResponse
            {
                Success = false,
                Message = $"Upload error: {ex.Message}"
            };
        }
    }

    public async Task<bool> ResetBackgroundImageAsync(int householdId)
    {
        try
        {
            var household = await _context.Households.FindAsync(householdId);
            if (household == null) return false;

            // Delete file if exists
            if (!string.IsNullOrEmpty(household.BackgroundImageUrl))
            {
                var filePath = Path.Combine("wwwroot", household.BackgroundImageUrl.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            household.BackgroundImageUrl = null;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class UploadImageResponse
{
    public bool Success { get; set; }
    public string? ImageUrl { get; set; }
    public string Message { get; set; } = string.Empty;
}
