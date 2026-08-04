using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Client.Services;

public class FamilyService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public FamilyService(HttpClient http, ILocalStorageService localStorage)
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

    public async Task<HouseholdInfoDto?> GetHouseholdInfoAsync()
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<HouseholdInfoDto>("api/family/household");
    }

    public async Task<bool> RemoveMemberAsync(int userId)
    {
        await AddAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/family/member/{userId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateMemberRoleAsync(UpdateMemberRoleRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("api/family/member/role", request);
        return response.IsSuccessStatusCode;
    }
}
