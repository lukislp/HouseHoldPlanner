using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Server.Services;
using HaushaltsPlaner.Server.Hubs;
using HaushaltsPlaner.Shared.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Configure forwarded headers for proxy support (Nginx)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add CORS - Allow all origins (for development)
// For production, restrict to specific origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
  {
      policy.SetIsOriginAllowed(origin => true)
          .AllowAnyMethod()
 .AllowAnyHeader()
.AllowCredentials()  // Required for SignalR
.WithExposedHeaders("*")
     .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
  });
});

// Add SignalR
builder.Services.AddSignalR();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
  ?? "Data Source=haushaltsplaner.db"));

// Add Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key must be configured (e.g. via the Jwt__Key environment variable).");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
  {
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "HaushaltsPlaner",
          ValidAudience = builder.Configuration["Jwt:Audience"] ?? "HaushaltsPlaner",
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
      };

      // SignalR requires the token from query string
      options.Events = new JwtBearerEvents
      {
          OnMessageReceived = context =>
          {
              var accessToken = context.Request.Query["access_token"];
              var path = context.HttpContext.Request.Path;

              // If the request targets the SignalR hub, read the token from query string
              if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
              {
                  context.Token = accessToken;
              }

              return Task.CompletedTask;
          }
      };
  });

builder.Services.AddAuthorization();

// Add Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<MealPlanService>();
builder.Services.AddScoped<FamilyService>();
builder.Services.AddScoped<HomeService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<RecipeService>();
builder.Services.AddScoped<RecipeImportService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Use forwarded headers FIRST (CRITICAL for Nginx proxy)
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS must be early in the pipeline
app.UseCors();

// Enable WebSockets (required for SignalR)
app.UseWebSockets();

// DO NOT use HTTPS redirection when behind a proxy
// The proxy handles HTTPS, the backend uses HTTP
// Comment out or remove this line when using Nginx:
// app.UseHttpsRedirection();

// Serve static files (for uploaded images)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Helper method to get user claims
static (int userId, int householdId) GetUserClaims(ClaimsPrincipal user)
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var householdIdClaim = user.FindFirst("HouseholdId")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(householdIdClaim))
        throw new UnauthorizedAccessException("Invalid token");

    return (int.Parse(userIdClaim), int.Parse(householdIdClaim));
}

// Get available households
app.MapGet("/api/households/available", (IConfiguration configuration) =>
{
    var households = configuration.GetSection("AvailableHouseholds")
 .Get<List<HouseholdOption>>() ?? new List<HouseholdOption>();
    return Results.Ok(households);
})
.WithName("GetAvailableHouseholds");

// Auth endpoints
app.MapPost("/api/auth/register", async (RegisterRequest request, AuthService authService) =>
{
    var result = await authService.RegisterAsync(request);
    return Results.Ok(result);
})
.WithName("Register");

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService authService) =>
{
    var result = await authService.LoginAsync(request);
    return Results.Ok(result);
})
.WithName("Login");

// TodoList endpoints
app.MapGet("/api/todos/lists", async (TodoService todoService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var lists = await todoService.GetTodoListsByHouseholdAsync(householdId);
    return Results.Ok(lists);
})
.RequireAuthorization()
.WithName("GetTodoLists");

app.MapGet("/api/todos/lists/{id}", async (int id, TodoService todoService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var list = await todoService.GetTodoListByIdAsync(id, householdId);
    return list != null ? Results.Ok(list) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetTodoListById");

app.MapPost("/api/todos/lists", async (CreateTodoListRequest request, TodoService todoService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var list = await todoService.CreateTodoListAsync(request, householdId, userId);
    return Results.Ok(list);
})
.RequireAuthorization()
.WithName("CreateTodoList");

app.MapPut("/api/todos/lists", async (UpdateTodoListRequest request, TodoService todoService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await todoService.UpdateTodoListAsync(request, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("UpdateTodoList");

app.MapDelete("/api/todos/lists/{id}", async (int id, TodoService todoService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await todoService.DeleteTodoListAsync(id, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("DeleteTodoList");

// TodoItem endpoints
app.MapGet("/api/todos/lists/{listId}/items", async (int listId, TodoService todoService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var items = await todoService.GetTodoItemsByListAsync(listId, householdId);
    return Results.Ok(items);
})
.RequireAuthorization()
.WithName("GetTodoItems");

app.MapPost("/api/todos/items", async (CreateTodoItemRequest request, TodoService todoService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var item = await todoService.CreateTodoItemAsync(request, householdId);
    return Results.Ok(item);
})
.RequireAuthorization()
.WithName("CreateTodoItem");

app.MapPut("/api/todos/items", async (UpdateTodoItemRequest request, TodoService todoService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var success = await todoService.UpdateTodoItemAsync(request, householdId, userId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("UpdateTodoItem");

app.MapPost("/api/todos/items/{id}/toggle", async (int id, ToggleTodoItemRequest request, TodoService todoService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var success = await todoService.ToggleTodoItemAsync(id, request.IsCompleted, householdId, userId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("ToggleTodoItem");

app.MapDelete("/api/todos/items/{id}", async (int id, TodoService todoService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await todoService.DeleteTodoItemAsync(id, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("DeleteTodoItem");

// Get household members for assignment
app.MapGet("/api/households/members", async (TodoService todoService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var members = await todoService.GetHouseholdMembersAsync(householdId);
    return Results.Ok(members);
})
.RequireAuthorization()
.WithName("GetHouseholdMembers");

// Calendar endpoints
app.MapGet("/api/calendar/month/{year}/{month}", async (int year, int month, CalendarService calendarService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var monthData = await calendarService.GetMonthEventsAsync(householdId, year, month);
    return Results.Ok(monthData);
})
.RequireAuthorization()
.WithName("GetCalendarMonth");

app.MapGet("/api/calendar/events", async (CalendarService calendarService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var events = await calendarService.GetAllEventsAsync(householdId);
    return Results.Ok(events);
})
.RequireAuthorization()
.WithName("GetAllCalendarEvents");

app.MapPost("/api/calendar/events", async (CreateCalendarEventRequest request, CalendarService calendarService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var calendarEvent = await calendarService.CreateEventAsync(request, householdId, userId);
    return Results.Ok(calendarEvent);
})
.RequireAuthorization()
.WithName("CreateCalendarEvent");

app.MapPut("/api/calendar/events", async (UpdateCalendarEventRequest request, CalendarService calendarService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await calendarService.UpdateEventAsync(request, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("UpdateCalendarEvent");

app.MapDelete("/api/calendar/events/{id}", async (int id, CalendarService calendarService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await calendarService.DeleteEventAsync(id, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("DeleteCalendarEvent");

// MealPlan endpoints
app.MapGet("/api/mealplan/week", async (DateTime startDate, MealPlanService mealPlanService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var weekData = await mealPlanService.GetWeekMealsAsync(householdId, startDate.Date);
    return Results.Ok(weekData);
})
.RequireAuthorization()
.WithName("GetMealPlanWeek");

app.MapGet("/api/mealplan/month/{year}/{month}", async (int year, int month, MealPlanService mealPlanService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var meals = await mealPlanService.GetMealsByMonthAsync(householdId, year, month);
    return Results.Ok(meals);
})
.RequireAuthorization()
.WithName("GetMealPlanMonth");

app.MapPost("/api/mealplan", async (CreateMealPlanRequest request, MealPlanService mealPlanService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var mealPlan = await mealPlanService.CreateMealPlanAsync(request, householdId, userId);
    return Results.Ok(mealPlan);
})
.RequireAuthorization()
.WithName("CreateMealPlan");

app.MapPut("/api/mealplan", async (UpdateMealPlanRequest request, MealPlanService mealPlanService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await mealPlanService.UpdateMealPlanAsync(request, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("UpdateMealPlan");

app.MapDelete("/api/mealplan/{id}", async (int id, MealPlanService mealPlanService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await mealPlanService.DeleteMealPlanAsync(id, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("DeleteMealPlan");

// Family endpoints
app.MapGet("/api/family/household", async (FamilyService familyService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var householdInfo = await familyService.GetHouseholdInfoAsync(householdId, userId);
    return householdInfo != null ? Results.Ok(householdInfo) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetHouseholdInfo");

app.MapDelete("/api/family/member/{userId}", async (int userId, FamilyService familyService, ClaimsPrincipal user) =>
{
    var (currentUserId, householdId) = GetUserClaims(user);
    var success = await familyService.RemoveMemberAsync(userId, householdId, currentUserId);
    return success ? Results.Ok() : Results.BadRequest();
})
.RequireAuthorization()
.WithName("RemoveMember");

app.MapPut("/api/family/member/role", async (UpdateMemberRoleRequest request, FamilyService familyService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await familyService.UpdateMemberRoleAsync(request, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("UpdateMemberRole");

// Home/Dashboard endpoints
app.MapGet("/api/home/stats", async (HomeService homeService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var stats = await homeService.GetDashboardStatsAsync(householdId, userId);
    return Results.Ok(stats);
})
.RequireAuthorization()
.WithName("GetDashboardStats");

// Upload background image
app.MapPost("/api/home/background", async (IFormFile file, HomeService homeService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var result = await homeService.UploadBackgroundImageAsync(householdId, file);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.RequireAuthorization()
.WithName("UploadBackgroundImage")
.DisableAntiforgery();

// Reset background image
app.MapDelete("/api/home/background", async (HomeService homeService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await homeService.ResetBackgroundImageAsync(householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("ResetBackgroundImage");

// Profile endpoints
app.MapGet("/api/profile", async (ProfileService profileService, ClaimsPrincipal user) =>
{
    var (userId, _) = GetUserClaims(user);
    var profile = await profileService.GetUserProfileAsync(userId);
    return profile != null ? Results.Ok(profile) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetUserProfile");

app.MapPut("/api/profile", async (UpdateProfileRequest request, ProfileService profileService, ClaimsPrincipal user) =>
{
    var (userId, _) = GetUserClaims(user);
    var success = await profileService.UpdateProfileAsync(userId, request);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("UpdateProfile");

app.MapPost("/api/profile/image", async (IFormFile file, ProfileService profileService, ClaimsPrincipal user) =>
{
    var (userId, _) = GetUserClaims(user);
    var result = await profileService.UploadProfileImageAsync(userId, file);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.RequireAuthorization()
.WithName("UploadProfileImage")
.DisableAntiforgery();

app.MapDelete("/api/profile/image", async (ProfileService profileService, ClaimsPrincipal user) =>
{
    var (userId, _) = GetUserClaims(user);
    var success = await profileService.DeleteProfileImageAsync(userId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("DeleteProfileImage");

// Recipe endpoints
app.MapGet("/api/recipes", async (RecipeService recipeService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var recipes = await recipeService.GetAllRecipesAsync(householdId);
    return Results.Ok(recipes);
})
.RequireAuthorization()
.WithName("GetAllRecipes");

app.MapGet("/api/recipes/{id}", async (int id, RecipeService recipeService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var recipe = await recipeService.GetRecipeByIdAsync(id, householdId);
    return recipe != null ? Results.Ok(recipe) : Results.NotFound();
})
.RequireAuthorization()
.WithName("GetRecipeById");

app.MapPost("/api/recipes", async (CreateRecipeRequest request, RecipeService recipeService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var recipe = await recipeService.CreateRecipeAsync(request, householdId, userId);
    return Results.Ok(recipe);
})
.RequireAuthorization()
.WithName("CreateRecipe");

app.MapPut("/api/recipes", async (UpdateRecipeRequest request, RecipeService recipeService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await recipeService.UpdateRecipeAsync(request, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("UpdateRecipe");

app.MapDelete("/api/recipes/{id}", async (int id, RecipeService recipeService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var success = await recipeService.DeleteRecipeAsync(id, householdId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("DeleteRecipe");

app.MapGet("/api/recipes/categories", async (RecipeService recipeService, ClaimsPrincipal user) =>
{
    var (_, householdId) = GetUserClaims(user);
    var categories = await recipeService.GetCategoriesAsync(householdId);
    return Results.Ok(categories);
})
.RequireAuthorization()
.WithName("GetRecipeCategories");

// Recipe import
app.MapPost("/api/recipes/import", async (ImportRecipeRequest request, RecipeImportService importService, ClaimsPrincipal user) =>
{
    var (_, _) = GetUserClaims(user);
    var result = await importService.ImportFromUrlAsync(request.Url);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.RequireAuthorization()
.WithName("ImportRecipe");

// Chat endpoints
app.MapGet("/api/chat/history", async (int skip, int take, ChatService chatService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var messages = await chatService.GetChatHistoryAsync(householdId, userId, skip, take);
    return Results.Ok(messages);
})
.RequireAuthorization()
.WithName("GetChatHistory");

app.MapGet("/api/chat/unread-count", async (ChatService chatService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var count = await chatService.GetUnreadCountAsync(householdId, userId);
    return Results.Ok(count);
})
.RequireAuthorization()
.WithName("GetUnreadCount");

app.MapPost("/api/chat/send", async (SendMessageRequest request, ChatService chatService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var message = await chatService.SendMessageAsync(request, householdId, userId);
    return Results.Ok(message);
})
.RequireAuthorization()
.WithName("SendChatMessage");

app.MapPost("/api/chat/mark-read/{messageId}", async (int messageId, ChatService chatService, ClaimsPrincipal user, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("MarkAsRead called: MessageId={MessageId}", messageId);

        var (userId, _) = GetUserClaims(user);
        logger.LogInformation("UserId from token: {UserId}", userId);

        var success = await chatService.MarkAsReadAsync(messageId, userId);
        logger.LogInformation("MarkAsRead result: {Success}", success);

        return success ? Results.Ok() : Results.NotFound();
    }
    catch (UnauthorizedAccessException ex)
    {
        logger.LogWarning("Unauthorized: {Message}", ex.Message);
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MarkAsRead error");
        return Results.StatusCode(500);
    }
})
.RequireAuthorization()
.WithName("MarkMessageAsRead");

app.MapDelete("/api/chat/{messageId}", async (int messageId, ChatService chatService, ClaimsPrincipal user) =>
{
    var (userId, householdId) = GetUserClaims(user);
    var success = await chatService.DeleteMessageAsync(messageId, householdId, userId);
    return success ? Results.Ok() : Results.NotFound();
})
.RequireAuthorization()
.WithName("DeleteChatMessage");

// SignalR hub must be mapped before app.Run() but after UseAuthorization()
app.MapHub<ChatHub>("/chathub");

// Test endpoint to verify SignalR hub is reachable
app.MapGet("/chathub/test", () => Results.Ok(new { status = "SignalR Hub is reachable" }))
    .WithName("TestChatHub")
    .AllowAnonymous();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
