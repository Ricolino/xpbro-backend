using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace XpBro.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid UserId()
    {
        var value = _http.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.Parse(value!);
    }

    public string Email()
    {
        return _http.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email)! ?? string.Empty;
    }

    public bool IsAuthenticated()
    {
        return _http.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
