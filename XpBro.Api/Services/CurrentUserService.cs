using System.Security.Claims;

namespace XpBro.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor http) : ICurrentUserService
{
    public Guid UserId()
    {
        string? value = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UnauthorizedAccessException("User id claim is missing.");
        }

        return Guid.Parse(value);
    }

    public string Email()
    {
        string? value = http.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UnauthorizedAccessException("Email claim is missing.");
        }

        return value;
    }

    public bool IsAuthenticated()
    {
        return http.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
