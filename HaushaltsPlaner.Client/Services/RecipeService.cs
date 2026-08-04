using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Client.Services;

public class RecipeService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public RecipeService(HttpClient http, ILocalStorageService localStorage)
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

    public async Task<List<RecipeListDto>> GetAllRecipesAsync()
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<RecipeListDto>>("api/recipes") ?? new();
    }

    public async Task<RecipeDto?> GetRecipeByIdAsync(int id)
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<RecipeDto>($"api/recipes/{id}");
    }

    public async Task<RecipeDto?> CreateRecipeAsync(CreateRecipeRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/recipes", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecipeDto>();
        }
        return null;
    }

    public async Task<bool> UpdateRecipeAsync(UpdateRecipeRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("api/recipes", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRecipeAsync(int id)
    {
        await AddAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/recipes/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<string>>("api/recipes/categories") ?? new();
    }

    public async Task<ImportRecipeResponse?> ImportFromUrlAsync(string url)
    {
        await AddAuthHeaderAsync();
        var request = new ImportRecipeRequest { Url = url };
        var response = await _http.PostAsJsonAsync("api/recipes/import", request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ImportRecipeResponse>();
        }

        return new ImportRecipeResponse
        {
            Success = false,
            Message = "Fehler beim Import"
        };
    }
}
