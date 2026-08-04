using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HaushaltsPlaner.Server.Data;
using HaushaltsPlaner.Server.Hubs;
using HaushaltsPlaner.Shared.DTOs;
using HaushaltsPlaner.Shared.Models;

namespace HaushaltsPlaner.Server.Services;

public class ChatService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatService> _logger;

    public ChatService(AppDbContext context, IHubContext<ChatHub> hubContext, ILogger<ChatService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<List<ChatMessageDto>> GetChatHistoryAsync(int householdId, int userId, int skip = 0, int take = 50)
    {
        var messages = await _context.Messages
        .Where(m => m.HouseholdId == householdId)
   .Where(m => m.RecipientUserId == null || // Broadcast-Nachrichten
             m.RecipientUserId == userId || // An mich
    m.SenderUserId == userId) // Von mir
          .Include(m => m.Sender)
         .Include(m => m.Recipient)
     .OrderByDescending(m => m.CreatedAt)
       .Skip(skip)
            .Take(take)
        .Select(m => new ChatMessageDto
        {
            Id = m.Id,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            IsRead = m.IsRead,
            ReadAt = m.ReadAt,
            SenderUserId = m.SenderUserId,
            SenderName = m.Sender.FullName,
            SenderProfileImageUrl = m.Sender.ProfileImageUrl,
            RecipientUserId = m.RecipientUserId,
            RecipientName = m.Recipient != null ? m.Recipient.FullName : null
        })
   .ToListAsync();

        return messages.OrderBy(m => m.CreatedAt).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int householdId, int userId)
    {
        return await _context.Messages
  .Where(m => m.HouseholdId == householdId)
  .Where(m => (m.RecipientUserId == null || m.RecipientUserId == userId) &&
    m.SenderUserId != userId &&
         !m.IsRead)
     .CountAsync();
    }

    public async Task<ChatMessageDto> SendMessageAsync(SendMessageRequest request, int householdId, int userId)
    {
        var message = new Message
        {
            Content = request.Content,
            HouseholdId = householdId,
            SenderUserId = userId,
            RecipientUserId = request.RecipientUserId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Load the full message with navigation properties
        var fullMessage = await _context.Messages
           .Include(m => m.Sender)
         .Include(m => m.Recipient)
            .FirstAsync(m => m.Id == message.Id);

        var messageDto = new ChatMessageDto
        {
            Id = fullMessage.Id,
            Content = fullMessage.Content,
            CreatedAt = fullMessage.CreatedAt,
            IsRead = fullMessage.IsRead,
            ReadAt = fullMessage.ReadAt,
            SenderUserId = fullMessage.SenderUserId,
            SenderName = fullMessage.Sender.FullName,
            SenderProfileImageUrl = fullMessage.Sender.ProfileImageUrl,
            RecipientUserId = fullMessage.RecipientUserId,
            RecipientName = fullMessage.Recipient?.FullName
        };

        // Send a real-time notification via SignalR
        await _hubContext.Clients.Group($"household_{householdId}")
     .SendAsync("ReceiveMessage", messageDto);

        _logger.LogInformation("Message {MessageId} sent to household {HouseholdId}", message.Id, householdId);

        return messageDto;
    }

    public async Task<bool> MarkAsReadAsync(int messageId, int userId)
    {
        var message = await _context.Messages
                   .FirstOrDefaultAsync(m => m.Id == messageId &&
           (m.RecipientUserId == userId || m.RecipientUserId == null));

        if (message == null || message.SenderUserId == userId)
            return false;

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Notify the sender via SignalR
        await _hubContext.Clients.Group($"household_{message.HouseholdId}")
       .SendAsync("MessageRead", messageId, userId);

        return true;
    }

    public async Task<bool> DeleteMessageAsync(int messageId, int householdId, int userId)
    {
        var message = await _context.Messages
           .FirstOrDefaultAsync(m => m.Id == messageId &&
          m.HouseholdId == householdId &&
          m.SenderUserId == userId);

        if (message == null)
            return false;

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        // Notify everyone via SignalR
        await _hubContext.Clients.Group($"household_{householdId}")
              .SendAsync("MessageDeleted", messageId);

        return true;
    }
}
