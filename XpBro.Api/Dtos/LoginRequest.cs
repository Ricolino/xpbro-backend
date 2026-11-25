namespace XpBro.Api.Dtos;

public sealed record LoginRequest
{
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";
}
