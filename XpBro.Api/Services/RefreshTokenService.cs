namespace XpBro.Api.Services;

using System.Security.Cryptography;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
