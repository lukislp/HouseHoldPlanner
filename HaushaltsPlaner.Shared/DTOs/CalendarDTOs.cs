namespace HaushaltsPlaner.Shared.DTOs;

public class CalendarEventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }

    // Recurrence
    public bool IsRecurring { get; set; }
    public string RecurrenceType { get; set; } = "None";
    public int? RecurrenceInterval { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }

    // User Info
    public int? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public string? CreatedByName { get; set; }
}

public class CreateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    public string? Color { get; set; }

    // Recurrence
    public bool IsRecurring { get; set; }
    public string RecurrenceType { get; set; } = "None";
    public int? RecurrenceInterval { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }

    public int? AssignedToUserId { get; set; }
}

public class UpdateCalendarEventRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    public string? Color { get; set; }
    public int? AssignedToUserId { get; set; }
}

public class CalendarMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<CalendarEventDto> Events { get; set; } = new();
}
