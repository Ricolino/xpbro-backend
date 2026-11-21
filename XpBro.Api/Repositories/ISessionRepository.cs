using XpBro.Api.Models;

namespace XpBro.Api.Repositories;

public interface ISessionRepository
{
    Task Create(
        Guid userId,
        Guid refreshTokenId,
        string? ipAddress,
        string? userAgent);

    Task<IEnumerable<UserSession>> GetByUserId(Guid userId);

    Task<UserSession?> Get(Guid sessionId);

    Task UpdateLastSeen(Guid sessionId);

    Task Revoke(Guid sessionId);

    Task RevokeAll(Guid userId);

    Task DeleteExpired();

    Task RevokeByRefreshTokenId(Guid refreshTokenId);
}
