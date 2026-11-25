namespace XpBro.Api.Dtos;

public sealed record UpdateUserRequest
{
    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    public string Email { get; set; } = "";
}
