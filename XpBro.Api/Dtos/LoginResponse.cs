namespace XpBro.Api.Dtos;

public sealed record LoginResponse
{
    public string AccessToken { get; set; } = "";
}
