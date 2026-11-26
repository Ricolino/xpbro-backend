namespace XpBro.Models.Users;

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = "";

    public string? PasswordHash { get; set; }

    public string? GoogleId { get; set; }

    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    public bool EmailConfirmed { get; set; }

    public bool IsActive { get; set; }
}
