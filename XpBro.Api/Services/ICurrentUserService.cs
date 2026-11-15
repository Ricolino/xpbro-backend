namespace XpBro.Api.Services;

public interface ICurrentUserService
{
    Guid UserId();
    string Email();
    bool IsAuthenticated();
}
