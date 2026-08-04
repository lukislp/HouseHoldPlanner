using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HaushaltsPlaner.Shared.DTOs;

namespace HaushaltsPlaner.Client.Services;

public class TodoService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public TodoService(HttpClient http, ILocalStorageService localStorage)
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

    // TodoList Methods
    public async Task<List<TodoListDto>> GetTodoListsAsync()
    {
        await AddAuthHeaderAsync();
        var response = await _http.GetFromJsonAsync<List<TodoListDto>>("api/todos/lists");
        return response ?? new List<TodoListDto>();
    }

    public async Task<TodoListDto?> GetTodoListByIdAsync(int id)
    {
        await AddAuthHeaderAsync();
        return await _http.GetFromJsonAsync<TodoListDto>($"api/todos/lists/{id}");
    }

    public async Task<TodoListDto?> CreateTodoListAsync(CreateTodoListRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/todos/lists", request);
        return response.IsSuccessStatusCode
      ? await response.Content.ReadFromJsonAsync<TodoListDto>()
     : null;
    }

    public async Task<bool> UpdateTodoListAsync(UpdateTodoListRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("api/todos/lists", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteTodoListAsync(int id)
    {
        await AddAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/todos/lists/{id}");
        return response.IsSuccessStatusCode;
    }

    // TodoItem Methods
    public async Task<List<TodoItemDto>> GetTodoItemsAsync(int listId)
    {
        await AddAuthHeaderAsync();
        var response = await _http.GetFromJsonAsync<List<TodoItemDto>>($"api/todos/lists/{listId}/items");
        return response ?? new List<TodoItemDto>();
    }

    public async Task<TodoItemDto?> CreateTodoItemAsync(CreateTodoItemRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/todos/items", request);
        return response.IsSuccessStatusCode
       ? await response.Content.ReadFromJsonAsync<TodoItemDto>()
          : null;
    }

    public async Task<bool> UpdateTodoItemAsync(UpdateTodoItemRequest request)
    {
        await AddAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync("api/todos/items", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleTodoItemAsync(int id, bool isCompleted)
    {
        await AddAuthHeaderAsync();
        var request = new ToggleTodoItemRequest { Id = id, IsCompleted = isCompleted };
        var response = await _http.PostAsJsonAsync($"api/todos/items/{id}/toggle", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteTodoItemAsync(int id)
    {
        await AddAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/todos/items/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserDto>> GetHouseholdMembersAsync()
    {
        await AddAuthHeaderAsync();
        var response = await _http.GetFromJsonAsync<List<UserDto>>("api/households/members");
        return response ?? new List<UserDto>();
    }
}
