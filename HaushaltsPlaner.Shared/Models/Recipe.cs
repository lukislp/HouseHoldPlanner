namespace HaushaltsPlaner.Shared.Models;

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Category { get; set; } // e.g. "Main Course", "Soup", "Dessert"
    public string? ImageUrl { get; set; }

    // Foreign Keys
    public int HouseholdId { get; set; }
    public int CreatedByUserId { get; set; }

    // Navigation Properties
    public Household Household { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? Unit { get; set; } // g, ml, pcs, tsp, tbsp, etc.
    public int SortOrder { get; set; }

    // Navigation Property
    public Recipe Recipe { get; set; } = null!;
}
