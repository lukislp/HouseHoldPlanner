using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Client.Services;

public class MealPlanService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public MealPlanService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task AddAuthHeaderAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            token = token.Trim('"');
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<MealPlanWeekDto?> GetWeekMealsAsync(DateTime startDate)
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<MealPlanWeekDto>($"api/mealplan/week?startDate={startDate:yyyy-MM-dd}");
    }

    public async Task<List<MealPlanDto>> GetMonthMealsAsync(int year, int month)
    {
        await AddAuthHeaderAsync();
        var response = await _http.GetFromJsonAsync<List<MealPlanDto>>($"api/mealplan/month/{year}/{month}");
        return response ?? new List<MealPlanDto>();
    }

    public async Task<MealPlanDto?> CreateMealPlanAsync(CreateMealPlanRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/mealplan", request);
        return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<MealPlanDto>()
             : null;
    }

    public async Task<bool> UpdateMealPlanAsync(UpdateMealPlanRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("api/mealplan", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteMealPlanAsync(int id)
    {
        await AddAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/mealplan/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserDto>> GetHouseholdMembersAsync()
    {
        await AddAuthHeaderAsync();
        var response = await _http.GetFromJsonAsync<List<UserDto>>("api/households/members");
        return response ?? new List<UserDto>();
    }
}
