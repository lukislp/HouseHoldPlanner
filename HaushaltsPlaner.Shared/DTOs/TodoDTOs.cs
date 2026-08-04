namespace HaushaltsPlaner.Shared.DTOs;

// TodoList DTOs
public class TodoListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public int ItemCount { get; set; }
    public int CompletedItemCount { get; set; }
}

public class CreateTodoListRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateTodoListRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// TodoItem DTOs
public class TodoItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TodoListId { get; set; }
    public string? AssignedToName { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? CompletedByName { get; set; }
}

public class CreateTodoItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public int? AssignedToUserId { get; set; }
    public int TodoListId { get; set; }
}

public class UpdateTodoItemRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public int? AssignedToUserId { get; set; }
    public bool IsCompleted { get; set; }
}

public class ToggleTodoItemRequest
{
    public int Id { get; set; }
    public bool IsCompleted { get; set; }
}
