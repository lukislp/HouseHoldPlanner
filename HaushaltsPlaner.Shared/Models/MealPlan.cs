namespace HaushaltsPlaner.Shared.Models;

public class MealPlan
{
    public int Id { get; set; }
    public string MealName { get; set; } = string.Empty;
    public string? Recipe { get; set; }
    public string? Ingredients { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; }
    public MealType MealType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public int HouseholdId { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; } // Who is cooking

    // Navigation Properties
    public Household Household { get; set; } = null!;
    public User? CreatedBy { get; set; }
    public User? AssignedTo { get; set; }
}

public enum MealType
{
    Lunch = 1,    // Lunch
    Dinner = 2    // Dinner
}
