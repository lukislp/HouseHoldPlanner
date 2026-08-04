namespace HaushaltsPlaner.Shared.Models;

public class Household
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BackgroundImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<User> Members { get; set; } = new List<User>();
}
