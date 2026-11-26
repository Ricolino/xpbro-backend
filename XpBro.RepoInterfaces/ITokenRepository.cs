using XpBro.Models.Auth;

namespace XpBro.RepoInterfaces;

public interface ITokenRepository
{
    Task Save(RefreshToken token);
    Task<RefreshToken?> Get(string token);
    Task Revoke(string token);
    Task Replace(string oldToken, string newToken);
    Task RevokeAll(Guid userId);
    Task DeleteExpired();
}
