using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HaushaltsPlaner.Shared.DTOs;
using Microsoft.AspNetCore.Components.Forms;

namespace HaushaltsPlaner.Client.Services;

public class ProfileService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public ProfileService(HttpClient http, ILocalStorageService localStorage)
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

    public async Task<UserProfileDto?> GetProfileAsync()
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<UserProfileDto>("api/profile");
    }

    public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("api/profile", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<UploadProfileImageResponse?> UploadProfileImageAsync(IBrowserFile file)
    {
        await AddAuthHeaderAsync();

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024)); // 5MB
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await _http.PostAsync("api/profile/image", content);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UploadProfileImageResponse>();
        }

        return new UploadProfileImageResponse
        {
            Success = false,
            ErrorMessage = "Fehler beim Hochladen"
        };
    }

    public async Task<bool> DeleteProfileImageAsync()
    {
        await AddAuthHeaderAsync();
        var response = await _http.DeleteAsync("api/profile/image");
        return response.IsSuccessStatusCode;
    }
}
