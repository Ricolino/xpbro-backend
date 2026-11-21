namespace XpBro.Api.Dtos;

public sealed class SessionResponse
{
    public Guid Id { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public bool IsActive { get; set; }
}
