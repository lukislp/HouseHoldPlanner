namespace HaushaltsPlaner.Shared.Models;

public class TodoList
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public int HouseholdId { get; set; }
    public int? CreatedByUserId { get; set; }

    // Navigation Properties
    public Household Household { get; set; } = null!;
    public User? CreatedBy { get; set; }
    public ICollection<TodoItem> Items { get; set; } = new List<TodoItem>();
}

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Foreign Keys
    public int TodoListId { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? CompletedByUserId { get; set; }

    // Navigation Properties
    public TodoList TodoList { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public User? CompletedBy { get; set; }
}
