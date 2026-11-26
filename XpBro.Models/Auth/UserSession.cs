namespace XpBro.Models.Auth;

public sealed class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid RefreshTokenId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
