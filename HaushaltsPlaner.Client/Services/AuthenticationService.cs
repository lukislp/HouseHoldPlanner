using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Client.Services;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public event Action? AuthenticationStateChanged;

    public AuthenticationService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
            {
                await _localStorage.SetItemAsync("authToken", result.Token);

                if (result.User != null)
                {
                    await _localStorage.SetItemAsync("user", result.User);
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                 new AuthenticationHeaderValue("Bearer", result.Token);

                try
                {
                    AuthenticationStateChanged?.Invoke();
                }
                catch
                {
                    // Ignore event errors on mobile
                }
            }

            return result ?? new AuthResponse { Success = false, Message = "Keine Antwort vom Server" };
        }
        catch (HttpRequestException ex)
        {
            return new AuthResponse { Success = false, Message = $"Netzwerkfehler: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Fehler: {ex.Message}" };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
            {
                await _localStorage.SetItemAsync("authToken", result.Token);

                if (result.User != null)
                {
                    await _localStorage.SetItemAsync("user", result.User);
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                      new AuthenticationHeaderValue("Bearer", result.Token);

                try
                {
                    AuthenticationStateChanged?.Invoke();
                }
                catch
                {
                    // Ignore event errors on mobile
                }
            }

            return result ?? new AuthResponse { Success = false, Message = "Keine Antwort vom Server" };
        }
        catch (HttpRequestException ex)
        {
            return new AuthResponse { Success = false, Message = $"Netzwerkfehler: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Fehler: {ex.Message}" };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("user");
            _httpClient.DefaultRequestHeaders.Authorization = null;

            try
            {
                AuthenticationStateChanged?.Invoke();
            }
            catch
            {
                // Ignore event errors
            }
        }
        catch
        {
            // Ensure logout completes even if storage fails
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            return !string.IsNullOrEmpty(token);
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<UserDto>("user");
        }
        catch
        {
            return null;
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // Initialization failure should not prevent app from starting
        }
    }

    public async Task<string?> GetUsernameAsync()
    {
        return await _localStorage.GetItemAsync<string>("username");
    }

    public async Task<int?> GetUserIdAsync()
    {
        try
        {
            // Get user object from local storage
            var user = await _localStorage.GetItemAsync<UserDto>("user");
            return user?.Id;
        }
        catch
        {
            return null;
        }
    }
}
