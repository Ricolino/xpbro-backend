namespace XpBro.Api.Dtos;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = "";

    public string RefreshToken { get; set; } = "";
}
