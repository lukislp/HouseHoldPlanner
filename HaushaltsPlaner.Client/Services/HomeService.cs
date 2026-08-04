using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HaushaltsPlaner.Shared.DTOs;
using Microsoft.AspNetCore.Components.Forms;

namespace HaushaltsPlaner.Client.Services;

public class HomeService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public HomeService(HttpClient http, ILocalStorageService localStorage)
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

    public async Task<DashboardStatsDto?> GetDashboardStatsAsync()
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<DashboardStatsDto>("api/home/stats");
    }

    public async Task<UploadImageResponse?> UploadBackgroundImageAsync(IBrowserFile file)
    {
        await AddAuthHeaderAsync();

        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)); // 10 MB
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _http.PostAsync("api/home/background", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UploadImageResponse>();
            }

            return new UploadImageResponse
            {
                Success = false,
                Message = $"Error: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new UploadImageResponse
            {
                Success = false,
                Message = $"Upload error: {ex.Message}"
            };
        }
    }

    public async Task<bool> ResetBackgroundImageAsync()
    {
        await AddAuthHeaderAsync();

        try
        {
            var response = await _http.DeleteAsync("api/home/background");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// Response DTO
public class UploadImageResponse
{
    public bool Success { get; set; }
    public string? ImageUrl { get; set; }
    public string Message { get; set; } = string.Empty;
}
