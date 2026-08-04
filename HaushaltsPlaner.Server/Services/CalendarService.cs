using Microsoft.EntityFrameworkCore;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Shared.DTOs;
using HaushaltsPlaner.Shared.Models;

namespace HaushaltsPlaner.Server.Services;

public class CalendarService
{
    private readonly AppDbContext _context;

    public CalendarService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CalendarMonthDto> GetMonthEventsAsync(int householdId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var events = await _context.CalendarEvents
      .Where(ce => ce.HouseholdId == householdId &&
          ce.StartDate >= startDate &&
      ce.StartDate <= endDate)
               .Include(ce => ce.AssignedTo)
               .Include(ce => ce.CreatedBy)
               .Select(ce => new CalendarEventDto
               {
                   Id = ce.Id,
                   Title = ce.Title,
                   Description = ce.Description,
                   StartDate = ce.StartDate,
                   EndDate = ce.EndDate,
                   IsAllDay = ce.IsAllDay,
                   Location = ce.Location,
                   Color = ce.Color,
                   CreatedAt = ce.CreatedAt,
                   IsRecurring = ce.IsRecurring,
                   RecurrenceType = ce.RecurrenceType.ToString(),
                   RecurrenceInterval = ce.RecurrenceInterval,
                   RecurrenceEndDate = ce.RecurrenceEndDate,
                   AssignedToUserId = ce.AssignedToUserId,
                   AssignedToName = ce.AssignedTo != null ? ce.AssignedTo.FullName : null,
                   CreatedByName = ce.CreatedBy != null ? ce.CreatedBy.FullName : null
               })
         .OrderBy(ce => ce.StartDate)
       .ToListAsync();

        // Generate recurring events for this month
        var recurringEvents = await GetRecurringEventsForMonth(householdId, year, month);
        events.AddRange(recurringEvents);

        return new CalendarMonthDto
        {
            Year = year,
            Month = month,
            Events = events.OrderBy(e => e.StartDate).ToList()
        };
    }

    private async Task<List<CalendarEventDto>> GetRecurringEventsForMonth(int householdId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var recurringEvents = await _context.CalendarEvents
                  .Where(ce => ce.HouseholdId == householdId &&
                ce.IsRecurring &&
      ce.StartDate < endDate &&
          (ce.RecurrenceEndDate == null || ce.RecurrenceEndDate >= startDate))
        .Include(ce => ce.AssignedTo)
           .Include(ce => ce.CreatedBy)
          .ToListAsync();

        var generatedEvents = new List<CalendarEventDto>();

        foreach (var baseEvent in recurringEvents)
        {
            var occurrences = GenerateOccurrences(baseEvent, startDate, endDate);
            generatedEvents.AddRange(occurrences);
        }

        return generatedEvents;
    }

    private List<CalendarEventDto> GenerateOccurrences(CalendarEvent baseEvent, DateTime rangeStart, DateTime rangeEnd)
    {
        var occurrences = new List<CalendarEventDto>();
        var current = baseEvent.StartDate;
        var interval = baseEvent.RecurrenceInterval ?? 1;

        while (current <= rangeEnd && (baseEvent.RecurrenceEndDate == null || current <= baseEvent.RecurrenceEndDate))
        {
            if (current >= rangeStart && current != baseEvent.StartDate) // Skip original event
            {
                occurrences.Add(new CalendarEventDto
                {
                    Id = baseEvent.Id, // Same ID for recurring instances
                    Title = baseEvent.Title,
                    Description = baseEvent.Description,
                    StartDate = current,
                    EndDate = baseEvent.EndDate.HasValue
    ? baseEvent.EndDate.Value.AddDays((current - baseEvent.StartDate).Days)
      : null,
                    IsAllDay = baseEvent.IsAllDay,
                    Location = baseEvent.Location,
                    Color = baseEvent.Color,
                    CreatedAt = baseEvent.CreatedAt,
                    IsRecurring = true,
                    RecurrenceType = baseEvent.RecurrenceType.ToString(),
                    RecurrenceInterval = baseEvent.RecurrenceInterval,
                    RecurrenceEndDate = baseEvent.RecurrenceEndDate,
                    AssignedToUserId = baseEvent.AssignedToUserId,
                    AssignedToName = baseEvent.AssignedTo?.FullName,
                    CreatedByName = baseEvent.CreatedBy?.FullName
                });
            }

            // Calculate next occurrence
            current = baseEvent.RecurrenceType switch
            {
                RecurrenceType.Daily => current.AddDays(interval),
                RecurrenceType.Weekly => current.AddDays(7 * interval),
                RecurrenceType.Monthly => current.AddMonths(interval),
                RecurrenceType.Yearly => current.AddYears(interval),
                _ => current.AddYears(100) // Break loop
            };
        }

        return occurrences;
    }

    public async Task<List<CalendarEventDto>> GetAllEventsAsync(int householdId)
    {
        return await _context.CalendarEvents
          .Where(ce => ce.HouseholdId == householdId)
                 .Include(ce => ce.AssignedTo)
                 .Include(ce => ce.CreatedBy)
        .Select(ce => new CalendarEventDto
        {
            Id = ce.Id,
            Title = ce.Title,
            Description = ce.Description,
            StartDate = ce.StartDate,
            EndDate = ce.EndDate,
            IsAllDay = ce.IsAllDay,
            Location = ce.Location,
            Color = ce.Color,
            CreatedAt = ce.CreatedAt,
            IsRecurring = ce.IsRecurring,
            RecurrenceType = ce.RecurrenceType.ToString(),
            RecurrenceInterval = ce.RecurrenceInterval,
            RecurrenceEndDate = ce.RecurrenceEndDate,
            AssignedToUserId = ce.AssignedToUserId,
            AssignedToName = ce.AssignedTo != null ? ce.AssignedTo.FullName : null,
            CreatedByName = ce.CreatedBy != null ? ce.CreatedBy.FullName : null
        })
                 .OrderBy(ce => ce.StartDate)
        .ToListAsync();
    }

    public async Task<CalendarEventDto> CreateEventAsync(CreateCalendarEventRequest request, int householdId, int userId)
    {
        var calendarEvent = new CalendarEvent
        {
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsAllDay = request.IsAllDay,
            Location = request.Location,
            Color = request.Color ?? GetDefaultColorForUser(request.AssignedToUserId),
            IsRecurring = request.IsRecurring,
            RecurrenceType = Enum.Parse<RecurrenceType>(request.RecurrenceType),
            RecurrenceInterval = request.RecurrenceInterval,
            RecurrenceEndDate = request.RecurrenceEndDate,
            HouseholdId = householdId,
            AssignedToUserId = request.AssignedToUserId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        var assignedTo = request.AssignedToUserId.HasValue
            ? await _context.Users.FindAsync(request.AssignedToUserId.Value)
         : null;
        var createdBy = await _context.Users.FindAsync(userId);

        return new CalendarEventDto
        {
            Id = calendarEvent.Id,
            Title = calendarEvent.Title,
            Description = calendarEvent.Description,
            StartDate = calendarEvent.StartDate,
            EndDate = calendarEvent.EndDate,
            IsAllDay = calendarEvent.IsAllDay,
            Location = calendarEvent.Location,
            Color = calendarEvent.Color,
            CreatedAt = calendarEvent.CreatedAt,
            IsRecurring = calendarEvent.IsRecurring,
            RecurrenceType = calendarEvent.RecurrenceType.ToString(),
            RecurrenceInterval = calendarEvent.RecurrenceInterval,
            RecurrenceEndDate = calendarEvent.RecurrenceEndDate,
            AssignedToUserId = assignedTo?.Id,
            AssignedToName = assignedTo?.FullName,
            CreatedByName = createdBy?.FullName
        };
    }

    public async Task<bool> UpdateEventAsync(UpdateCalendarEventRequest request, int householdId)
    {
        var calendarEvent = await _context.CalendarEvents
     .FirstOrDefaultAsync(ce => ce.Id == request.Id && ce.HouseholdId == householdId);

        if (calendarEvent == null) return false;

        calendarEvent.Title = request.Title;
        calendarEvent.Description = request.Description;
        calendarEvent.StartDate = request.StartDate;
        calendarEvent.EndDate = request.EndDate;
        calendarEvent.IsAllDay = request.IsAllDay;
        calendarEvent.Location = request.Location;
        calendarEvent.Color = request.Color;
        calendarEvent.AssignedToUserId = request.AssignedToUserId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteEventAsync(int id, int householdId)
    {
        var calendarEvent = await _context.CalendarEvents
   .FirstOrDefaultAsync(ce => ce.Id == id && ce.HouseholdId == householdId);

        if (calendarEvent == null) return false;

        _context.CalendarEvents.Remove(calendarEvent);
        await _context.SaveChangesAsync();
        return true;
    }

    private string GetDefaultColorForUser(int? userId)
    {
        if (!userId.HasValue) return "#667eea";

        var colors = new[] { "#667eea", "#f093fb", "#4facfe", "#43e97b", "#fa709a", "#30cfd0", "#a8edea", "#feb692" };
        return colors[userId.Value % colors.Length];
    }
}
