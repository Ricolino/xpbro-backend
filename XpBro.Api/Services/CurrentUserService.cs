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
        string? value = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UnauthorizedAccessException("User id claim is missing.");
        }

        return Guid.Parse(value);
    }

    public string Email()
    {
        string? value = _http.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UnauthorizedAccessException("Email claim is missing.");
        }

        return value;
    }

    public bool IsAuthenticated()
    {
        return _http.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
