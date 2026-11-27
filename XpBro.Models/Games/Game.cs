namespace XpBro.Models.Games;

public class Game
{
    public Guid Id { get; set; }

    public Guid PlatformId { get; set; }

    public string ExternalGameId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string Metadata { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
