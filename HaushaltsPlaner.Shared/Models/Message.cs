namespace HaushaltsPlaner.Shared.Models;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // Foreign Keys
    public int HouseholdId { get; set; }
    public int SenderUserId { get; set; }
    public int? RecipientUserId { get; set; } // null = to all household members

    // Navigation Properties
    public Household Household { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public User? Recipient { get; set; }
}
