namespace XpBro.Models.Platforms;

public class Platform
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string AuthType { get; set; } = "public";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
