using Microsoft.EntityFrameworkCore;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Shared.DTOs;
using HaushaltsPlaner.Shared.Models;

namespace HaushaltsPlaner.Server.Services;

public class MealPlanService
{
    private readonly AppDbContext _context;

    public MealPlanService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MealPlanWeekDto> GetWeekMealsAsync(int householdId, DateTime startDate)
    {
        var endDate = startDate.AddDays(6);

        var meals = await _context.MealPlans
       .Where(mp => mp.HouseholdId == householdId &&
           mp.Date >= startDate &&
        mp.Date <= endDate)
     .Include(mp => mp.AssignedTo)
                 .Include(mp => mp.CreatedBy)
             .OrderBy(mp => mp.Date)
                 .ThenBy(mp => mp.MealType)
       .ToListAsync();

        var mealDtos = meals.Select(mp => new MealPlanDto
        {
            Id = mp.Id,
            MealName = mp.MealName,
            Recipe = mp.Recipe,
            Ingredients = mp.Ingredients,
            Notes = mp.Notes,
            Date = mp.Date,
            MealType = mp.MealType.ToString(),
            CreatedAt = mp.CreatedAt,
            AssignedToUserId = mp.AssignedToUserId,
            AssignedToName = mp.AssignedTo != null ? mp.AssignedTo.FullName : null,
            CreatedByName = mp.CreatedBy != null ? mp.CreatedBy.FullName : null
        })
 .ToList();

        return new MealPlanWeekDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Meals = mealDtos
        };
    }

    public async Task<List<MealPlanDto>> GetMealsByMonthAsync(int householdId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var meals = await _context.MealPlans
     .Where(mp => mp.HouseholdId == householdId &&
      mp.Date >= startDate &&
    mp.Date <= endDate)
        .Include(mp => mp.AssignedTo)
          .Include(mp => mp.CreatedBy)
      .OrderBy(mp => mp.Date)
           .ThenBy(mp => mp.MealType)
          .ToListAsync();

        return meals.Select(mp => new MealPlanDto
        {
            Id = mp.Id,
            MealName = mp.MealName,
            Recipe = mp.Recipe,
            Ingredients = mp.Ingredients,
            Notes = mp.Notes,
            Date = mp.Date,
            MealType = mp.MealType.ToString(),
            CreatedAt = mp.CreatedAt,
            AssignedToUserId = mp.AssignedToUserId,
            AssignedToName = mp.AssignedTo != null ? mp.AssignedTo.FullName : null,
            CreatedByName = mp.CreatedBy != null ? mp.CreatedBy.FullName : null
        })
         .ToList();
    }

    public async Task<MealPlanDto> CreateMealPlanAsync(CreateMealPlanRequest request, int householdId, int userId)
    {
        var mealPlan = new MealPlan
        {
            MealName = request.MealName,
            Recipe = request.Recipe,
            Ingredients = request.Ingredients,
            Notes = request.Notes,
            Date = request.Date.Date, // Only date, no time
            MealType = Enum.Parse<MealType>(request.MealType),
            HouseholdId = householdId,
            AssignedToUserId = request.AssignedToUserId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.MealPlans.Add(mealPlan);
        await _context.SaveChangesAsync();

        var assignedTo = request.AssignedToUserId.HasValue
            ? await _context.Users.FindAsync(request.AssignedToUserId.Value)
  : null;
        var createdBy = await _context.Users.FindAsync(userId);

        return new MealPlanDto
        {
            Id = mealPlan.Id,
            MealName = mealPlan.MealName,
            Recipe = mealPlan.Recipe,
            Ingredients = mealPlan.Ingredients,
            Notes = mealPlan.Notes,
            Date = mealPlan.Date,
            MealType = mealPlan.MealType.ToString(),
            CreatedAt = mealPlan.CreatedAt,
            AssignedToUserId = assignedTo?.Id,
            AssignedToName = assignedTo?.FullName,
            CreatedByName = createdBy?.FullName
        };
    }

    public async Task<bool> UpdateMealPlanAsync(UpdateMealPlanRequest request, int householdId)
    {
        var mealPlan = await _context.MealPlans
                  .FirstOrDefaultAsync(mp => mp.Id == request.Id && mp.HouseholdId == householdId);

        if (mealPlan == null) return false;

        mealPlan.MealName = request.MealName;
        mealPlan.Recipe = request.Recipe;
        mealPlan.Ingredients = request.Ingredients;
        mealPlan.Notes = request.Notes;
        mealPlan.Date = request.Date.Date;
        mealPlan.MealType = Enum.Parse<MealType>(request.MealType);
        mealPlan.AssignedToUserId = request.AssignedToUserId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMealPlanAsync(int id, int householdId)
    {
        var mealPlan = await _context.MealPlans
              .FirstOrDefaultAsync(mp => mp.Id == id && mp.HouseholdId == householdId);

        if (mealPlan == null) return false;

        _context.MealPlans.Remove(mealPlan);
        await _context.SaveChangesAsync();
        return true;
    }
}
