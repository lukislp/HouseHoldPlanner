namespace HaushaltsPlaner.Shared.DTOs;

public class MealPlanDto
{
    public int Id { get; set; }
    public string MealName { get; set; } = string.Empty;
    public string? Recipe { get; set; }
    public string? Ingredients { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; }
    public string MealType { get; set; } = "Lunch";
    public DateTime CreatedAt { get; set; }

    public int? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public string? CreatedByName { get; set; }
}

public class CreateMealPlanRequest
{
    public string MealName { get; set; } = string.Empty;
    public string? Recipe { get; set; }
    public string? Ingredients { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; }
    public string MealType { get; set; } = "Lunch";
    public int? AssignedToUserId { get; set; }
}

public class UpdateMealPlanRequest
{
    public int Id { get; set; }
    public string MealName { get; set; } = string.Empty;
    public string? Recipe { get; set; }
    public string? Ingredients { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; }
    public string MealType { get; set; } = "Lunch";
    public int? AssignedToUserId { get; set; }
}

public class MealPlanWeekDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<MealPlanDto> Meals { get; set; } = new();
}
