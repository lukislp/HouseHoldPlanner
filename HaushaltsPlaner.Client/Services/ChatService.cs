using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Client.Services;

public class ChatService : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;
    private HubConnection? _hubConnection;

    public event Action<ChatMessageDto>? OnMessageReceived;
    public event Action<int, int>? OnMessageRead;
    public event Action<int>? OnMessageDeleted;
    public event Action<string, bool>? OnUserTyping;
    public event Action? OnReconnected;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public ChatService(HttpClient http, ILocalStorageService localStorage, NavigationManager navigation)
    {
        _http = http;
        _localStorage = localStorage;
        _navigation = navigation;
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

    public async Task StartConnectionAsync()
    {
        if (_hubConnection != null)
            return;

        var token = await _localStorage.GetItemAsStringAsync("authToken");
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("? No auth token found");
            return;
        }

        token = token.Trim('"');

        // Get server URL from config or fall back to Navigation.BaseUri
        var serverUrl = _http.BaseAddress?.ToString().TrimEnd('/') ?? _navigation.BaseUri.TrimEnd('/');

        _hubConnection = new HubConnectionBuilder()
        .WithUrl($"{serverUrl}/chathub", options =>
   {
       options.AccessTokenProvider = () => Task.FromResult<string?>(token);
       Console.WriteLine($"Token provided: {token.Substring(0, Math.Min(20, token.Length))}...");
   })
  .WithAutomaticReconnect()
  .Build();

        // Event-Handler registrieren
        _hubConnection.On<ChatMessageDto>("ReceiveMessage", (message) =>
          {
              OnMessageReceived?.Invoke(message);
          });

        _hubConnection.On<int, int>("MessageRead", (messageId, userId) =>
       {
           OnMessageRead?.Invoke(messageId, userId);
       });

        _hubConnection.On<int>("MessageDeleted", (messageId) =>
     {
         OnMessageDeleted?.Invoke(messageId);
     });

        _hubConnection.On<string, bool>("UserTyping", (userName, isTyping) =>
              {
                  OnUserTyping?.Invoke(userName, isTyping);
              });

        _hubConnection.Reconnected += async (connectionId) =>
     {
         OnReconnected?.Invoke();
         await Task.CompletedTask;
     };

        try
        {
            await _hubConnection.StartAsync();
            Console.WriteLine("? SignalR Connected!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to SignalR: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }

    public async Task StopConnectionAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task<List<ChatMessageDto>> GetChatHistoryAsync(int skip = 0, int take = 50)
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<ChatMessageDto>>($"api/chat/history?skip={skip}&take={take}") ?? new();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<int>("api/chat/unread-count");
    }

    public async Task<ChatMessageDto?> SendMessageAsync(SendMessageRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/chat/send", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ChatMessageDto>();
        }
        return null;
    }

    public async Task<bool> MarkAsReadAsync(int messageId)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PostAsync($"api/chat/mark-read/{messageId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteMessageAsync(int messageId)
    {
        await AddAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/chat/{messageId}");
        return response.IsSuccessStatusCode;
    }

    public async Task SendTypingIndicatorAsync(bool isTyping)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("UserTyping", isTyping);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending typing indicator: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopConnectionAsync();
    }
}
