using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Client.Services;

public class CalendarService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public CalendarService(HttpClient http, ILocalStorageService localStorage)
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

    public async Task<CalendarMonthDto?> GetMonthEventsAsync(int year, int month)
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<CalendarMonthDto>($"api/calendar/month/{year}/{month}");
    }

    public async Task<List<CalendarEventDto>> GetAllEventsAsync()
    {
        await AddAuthHeaderAsync();
        var response = await _http.GetFromJsonAsync<List<CalendarEventDto>>("api/calendar/events");
        return response ?? new List<CalendarEventDto>();
    }

    public async Task<CalendarEventDto?> CreateEventAsync(CreateCalendarEventRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/calendar/events", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CalendarEventDto>()
 : null;
    }

    public async Task<bool> UpdateEventAsync(UpdateCalendarEventRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("api/calendar/events", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        await AddAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/calendar/events/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserDto>> GetHouseholdMembersAsync()
    {
        await AddAuthHeaderAsync();
        var response = await _http.GetFromJsonAsync<List<UserDto>>("api/households/members");
        return response ?? new List<UserDto>();
    }
}
