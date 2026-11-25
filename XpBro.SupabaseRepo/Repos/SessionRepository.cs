using Microsoft.Extensions.Configuration;
using Npgsql;
using XpBro.Models.Domain;
using XpBro.RepoInterfaces;

namespace XpBro.SupabaseRepo.Repos;

public sealed class SessionRepository(IConfiguration configuration) : ISessionRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("Db")!;

    public async Task Create(Guid userId, Guid refreshTokenId, string? ipAddress, string? userAgent)
    {
        const string sql = """
            insert into xpbro.user_sessions
            (
                id,
                user_id,
                refresh_token_id,
                ip_address,
                user_agent,
                created_at,
                last_seen_at
            )
            values
            (
                @id,
                @userId,
                @refreshTokenId,
                @ipAddress,
                @userAgent,
                now(),
                now()
            );
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("refreshTokenId", refreshTokenId);
        command.Parameters.AddWithValue("ipAddress", (object?)ipAddress ?? DBNull.Value);

        command.Parameters.AddWithValue("userAgent", (object?)userAgent ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<UserSession>> GetByUserId(Guid userId)
    {
        const string sql = """
            select
                id,
                user_id,
                refresh_token_id,
                ip_address,
                user_agent,
                created_at,
                last_seen_at,
                revoked_at
            from xpbro.user_sessions
            where user_id = @userId
            order by last_seen_at desc;
            """;

        List<UserSession> sessions = [];

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            sessions.Add(new UserSession
            {
                Id = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                RefreshTokenId = reader.GetGuid(2),
                IpAddress = reader.IsDBNull(3) ? null : reader.GetString(3),
                UserAgent = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetDateTime(5),
                LastSeenAt = reader.GetDateTime(6),
                RevokedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            });
        }

        return sessions;
    }

    public async Task<UserSession?> Get(Guid sessionId)
    {
        const string sql = """
            select
                id,
                user_id,
                refresh_token_id,
                ip_address,
                user_agent,
                created_at,
                last_seen_at,
                revoked_at
            from xpbro.user_sessions
            where id = @id;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", sessionId);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new UserSession
        {
            Id = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            RefreshTokenId = reader.GetGuid(2),
            IpAddress = reader.IsDBNull(3)
                ? null
                : reader.GetString(3),
            UserAgent = reader.IsDBNull(4)
                ? null
                : reader.GetString(4),
            CreatedAt = reader.GetDateTime(5),
            LastSeenAt = reader.GetDateTime(6),
            RevokedAt = reader.IsDBNull(7)
                ? null
                : reader.GetDateTime(7)
        };
    }

    public async Task UpdateLastSeen(Guid sessionId)
    {
        const string sql = """
            update xpbro.user_sessions
            set last_seen_at = now()
            where id = @sessionId;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("sessionId", sessionId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task Revoke(Guid sessionId)
    {
        const string sql = """
            update xpbro.user_sessions
            set revoked_at = now()
            where id = @sessionId;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("sessionId", sessionId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RevokeAll(Guid userId)
    {
        const string sql = """
            update xpbro.user_sessions
            set revoked_at = now()
            where user_id = @userId
            and revoked_at is null;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("userId", userId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteExpired()
    {
        const string sql = """
            delete from xpbro.user_sessions
            where revoked_at is not null
            and revoked_at < now() - interval '30 days';
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RevokeByRefreshTokenId(Guid refreshTokenId)
    {
        const string sql = """
        update xpbro.user_sessions
        set revoked_at = now()
        where refresh_token_id = @refreshTokenId;
        """;

        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("refreshTokenId", refreshTokenId);

        await command.ExecuteNonQueryAsync();
    }
}
