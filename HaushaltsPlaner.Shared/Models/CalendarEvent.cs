namespace HaushaltsPlaner.Shared.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    public string? Color { get; set; } // Color used in the UI
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Recurrence
    public bool IsRecurring { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int? RecurrenceInterval { get; set; } // e.g. every 2nd week
    public DateTime? RecurrenceEndDate { get; set; }
    public int? ParentEventId { get; set; } // For generated recurring events

    // Foreign Keys
    public int HouseholdId { get; set; }
    public int? AssignedToUserId { get; set; } // Assigned to person
    public int? CreatedByUserId { get; set; }

    // Navigation Properties
    public Household Household { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public User? CreatedBy { get; set; }
}

public enum RecurrenceType
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Yearly = 4
}
