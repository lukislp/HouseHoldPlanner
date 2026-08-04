using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HaushaltsPlaner.Client;
using HaushaltsPlaner.Client.Services;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get server URL from configuration or use default
var serverUrl = builder.Configuration["ServerUrl"];
if (string.IsNullOrWhiteSpace(serverUrl))
    serverUrl = builder.HostEnvironment.BaseAddress;

// Configure HttpClient
// Note: In Blazor WebAssembly, the browser handles all certificate validation
// Custom certificate validation callbacks are NOT supported in the browser
// Users must accept the certificate in their browser before the app will work with HTTPS
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(serverUrl),
    // Increase timeout for mobile devices
    Timeout = TimeSpan.FromSeconds(100)
});

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// i18n: TranslationStore is a singleton loaded once; I18nService is scoped per user session
builder.Services.AddSingleton<TranslationStore>();
builder.Services.AddScoped<I18nService>();

// Add Authentication Service
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<MealPlanService>();
builder.Services.AddScoped<FamilyService>();
builder.Services.AddScoped<HomeService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<RecipeService>();
builder.Services.AddScoped<ChatService>();

// Remember the client's BaseAddress before Build() is called
var clientBaseAddress = builder.HostEnvironment.BaseAddress;

var host = builder.Build();

// Initialize authentication
var authService = host.Services.GetRequiredService<AuthenticationService>();
await authService.InitializeAsync();

// Load i18n translation files (static files from wwwroot -> Client BaseAddress, not Server)
var translationStore = host.Services.GetRequiredService<TranslationStore>();
using var staticClient = new HttpClient { BaseAddress = new Uri(clientBaseAddress) };
await translationStore.LoadAsync(staticClient);

await host.RunAsync();
