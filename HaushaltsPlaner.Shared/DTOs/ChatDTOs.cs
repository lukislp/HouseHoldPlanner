namespace HaushaltsPlaner.Shared.DTOs;

public class ChatMessageDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public int SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderProfileImageUrl { get; set; }

    public int? RecipientUserId { get; set; }
    public string? RecipientName { get; set; }
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public int? RecipientUserId { get; set; } // null = to all
}

public class MarkMessageAsReadRequest
{
    public int MessageId { get; set; }
}

public class ChatHistoryResponse
{
    public List<ChatMessageDto> Messages { get; set; } = new();
    public int UnreadCount { get; set; }
}
