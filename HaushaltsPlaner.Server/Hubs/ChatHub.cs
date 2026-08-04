using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace HaushaltsPlaner.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var householdId = Context.User?.FindFirst("HouseholdId")?.Value;

            _logger.LogInformation("SignalR Connection attempt - UserId: {UserId}, HouseholdId: {HouseholdId}, ConnectionId: {ConnectionId}",
                 userId, householdId, Context.ConnectionId);

            if (!string.IsNullOrEmpty(householdId))
            {
                // Add the user to the household group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"household_{householdId}");
                _logger.LogInformation("User {UserId} joined household {HouseholdId} chat group", userId, householdId);
            }
            else
            {
                _logger.LogWarning("User connected without HouseholdId claim");
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var householdId = Context.User?.FindFirst("HouseholdId")?.Value;

        if (!string.IsNullOrEmpty(householdId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"household_{householdId}");
            _logger.LogInformation("User {UserId} left household {HouseholdId} chat", userId, householdId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Client calls this method to send a message
    public async Task SendMessage(string content, int? recipientUserId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var householdId = Context.User?.FindFirst("HouseholdId")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(householdId))
        {
            throw new HubException("Unauthorized");
        }

        _logger.LogInformation("User {UserId} sending message to household {HouseholdId}", userId, householdId);

        // The actual message is processed by the server-side service
        // This hub only forwards the real-time notification
    }

    // Server calls this method to notify clients
    public async Task NotifyNewMessage(int householdId, object messageDto)
    {
        await Clients.Group($"household_{householdId}").SendAsync("ReceiveMessage", messageDto);
    }

    // Notification about a read message
    public async Task NotifyMessageRead(int householdId, int messageId, int userId)
    {
        await Clients.Group($"household_{householdId}").SendAsync("MessageRead", messageId, userId);
    }

    // Typing-Indicator
    public async Task UserTyping(bool isTyping)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var householdId = Context.User?.FindFirst("HouseholdId")?.Value;
        var userName = Context.User?.FindFirst("FullName")?.Value ??
    Context.User?.Identity?.Name ?? "Unbekannt";

        if (!string.IsNullOrEmpty(householdId))
        {
            await Clients.OthersInGroup($"household_{householdId}")
                  .SendAsync("UserTyping", userName, isTyping);
        }
    }
}
