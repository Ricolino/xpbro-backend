using XpBro.Api.Models;

namespace XpBro.Api.Repositories;

public interface ITokenRepository
{
    Task Save(RefreshToken token);
    Task<RefreshToken?> Get(string token);
    Task Revoke(string token);
    Task Replace(string oldToken, string newToken);
    Task RevokeAll(Guid userId);
    Task DeleteExpired();
}
