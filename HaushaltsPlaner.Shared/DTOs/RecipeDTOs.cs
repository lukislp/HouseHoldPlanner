namespace HaushaltsPlaner.Shared.DTOs;

public class RecipeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? CreatedByName { get; set; }
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class RecipeIngredientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? Unit { get; set; }
    public int SortOrder { get; set; }
}

public class CreateRecipeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Category { get; set; }
    public List<CreateRecipeIngredientRequest> Ingredients { get; set; } = new();
}

public class CreateRecipeIngredientRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? Unit { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateRecipeRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Category { get; set; }
    public List<CreateRecipeIngredientRequest> Ingredients { get; set; } = new();
}

public class RecipeListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? ImageUrl { get; set; }
    public int IngredientsCount { get; set; }
}

// DTOs for recipe import
public class ImportRecipeRequest
{
    public string Url { get; set; } = string.Empty;
}

public class ImportRecipeResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public CreateRecipeRequest? Recipe { get; set; }
}

public class RecipeImportPreviewDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public string? Source { get; set; }
    public List<CreateRecipeIngredientRequest> Ingredients { get; set; } = new();
}
