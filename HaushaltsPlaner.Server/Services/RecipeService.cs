using Microsoft.EntityFrameworkCore;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Shared.DTOs;
using HaushaltsPlaner.Shared.Models;

namespace HaushaltsPlaner.Server.Services;

public class RecipeService
{
    private readonly AppDbContext _context;

    public RecipeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecipeListDto>> GetAllRecipesAsync(int householdId)
    {
        return await _context.Recipes
            .Where(r => r.HouseholdId == householdId)
  .Include(r => r.Ingredients)
        .OrderBy(r => r.Name)
         .Select(r => new RecipeListDto
         {
             Id = r.Id,
             Name = r.Name,
             Description = r.Description,
             Category = r.Category,
             PrepTimeMinutes = r.PrepTimeMinutes,
             CookTimeMinutes = r.CookTimeMinutes,
             Servings = r.Servings,
             ImageUrl = r.ImageUrl,
             IngredientsCount = r.Ingredients.Count
         })
          .ToListAsync();
    }

    public async Task<RecipeDto?> GetRecipeByIdAsync(int id, int householdId)
    {
        var recipe = await _context.Recipes
           .Where(r => r.Id == id && r.HouseholdId == householdId)
         .Include(r => r.Ingredients)
              .Include(r => r.CreatedBy)
          .FirstOrDefaultAsync();

        if (recipe == null) return null;

        return new RecipeDto
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            Instructions = recipe.Instructions,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookTimeMinutes = recipe.CookTimeMinutes,
            Servings = recipe.Servings,
            Category = recipe.Category,
            ImageUrl = recipe.ImageUrl,
            CreatedByName = recipe.CreatedBy?.FullName,
            CreatedAt = recipe.CreatedAt,
            Ingredients = recipe.Ingredients
               .OrderBy(i => i.SortOrder)
      .Select(i => new RecipeIngredientDto
      {
          Id = i.Id,
          Name = i.Name,
          Amount = i.Amount,
          Unit = i.Unit,
          SortOrder = i.SortOrder
      })
          .ToList()
        };
    }

    public async Task<RecipeDto> CreateRecipeAsync(CreateRecipeRequest request, int householdId, int userId)
    {
        var recipe = new Recipe
        {
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions,
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            Servings = request.Servings,
            Category = request.Category,
            HouseholdId = householdId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        // Add ingredients
        foreach (var ing in request.Ingredients)
        {
            var ingredient = new RecipeIngredient
            {
                RecipeId = recipe.Id,
                Name = ing.Name,
                Amount = ing.Amount,
                Unit = ing.Unit,
                SortOrder = ing.SortOrder
            };
            _context.RecipeIngredients.Add(ingredient);
        }

        await _context.SaveChangesAsync();

        // Reload with includes
        return (await GetRecipeByIdAsync(recipe.Id, householdId))!;
    }

    public async Task<bool> UpdateRecipeAsync(UpdateRecipeRequest request, int householdId)
    {
        var recipe = await _context.Recipes
    .Include(r => r.Ingredients)
  .FirstOrDefaultAsync(r => r.Id == request.Id && r.HouseholdId == householdId);

        if (recipe == null) return false;

        recipe.Name = request.Name;
        recipe.Description = request.Description;
        recipe.Instructions = request.Instructions;
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.CookTimeMinutes = request.CookTimeMinutes;
        recipe.Servings = request.Servings;
        recipe.Category = request.Category;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Remove old ingredients
        _context.RecipeIngredients.RemoveRange(recipe.Ingredients);

        // Add new ingredients
        foreach (var ing in request.Ingredients)
        {
            var ingredient = new RecipeIngredient
            {
                RecipeId = recipe.Id,
                Name = ing.Name,
                Amount = ing.Amount,
                Unit = ing.Unit,
                SortOrder = ing.SortOrder
            };
            _context.RecipeIngredients.Add(ingredient);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRecipeAsync(int id, int householdId)
    {
        var recipe = await _context.Recipes
     .FirstOrDefaultAsync(r => r.Id == id && r.HouseholdId == householdId);

        if (recipe == null) return false;

        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<string>> GetCategoriesAsync(int householdId)
    {
        return await _context.Recipes
     .Where(r => r.HouseholdId == householdId && r.Category != null)
            .Select(r => r.Category!)
    .Distinct()
     .OrderBy(c => c)
            .ToListAsync();
    }
}
