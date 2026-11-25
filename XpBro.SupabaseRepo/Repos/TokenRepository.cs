using Microsoft.Extensions.Configuration;
using Npgsql;
using XpBro.Models.Domain;
using XpBro.RepoInterfaces;

namespace XpBro.SupabaseRepo.Repos;

public sealed class TokenRepository(IConfiguration configuration) : ITokenRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("Db")!;

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task Save(RefreshToken token)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
    insert into xpbro.refresh_tokens
    (
        id,
        user_id,
        token,
        expires_at,
        created_at
    )
    values
    (
        @id,
        @userId,
        @token,
        @expiresAt,
        now()
    )
    """;

        command.Parameters.AddWithValue("id", token.Id);

        command.Parameters.AddWithValue("userId", token.UserId);

        command.Parameters.AddWithValue("token", token.Token);

        command.Parameters.AddWithValue("expiresAt", token.ExpiresAt);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<RefreshToken?> Get(string token)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
    select
        id,
        user_id,
        token,
        expires_at,
        revoked_at,
        replaced_by_token
    from xpbro.refresh_tokens
    where token=@token
    """;

        command.Parameters.AddWithValue("token", token);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new RefreshToken
        {
            Id = reader.GetGuid(0),

            UserId = reader.GetGuid(1),

            Token = reader.GetString(2),

            ExpiresAt = reader.GetDateTime(3),

            RevokedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),

            ReplacedByToken = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    public async Task Revoke(string token)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
    update xpbro.refresh_tokens

    set revoked_at = now()

    where token = @token
    """;

        command.Parameters.AddWithValue("token", token);

        await command.ExecuteNonQueryAsync();
    }

    public async Task Replace(string oldToken, string newToken)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
    update xpbro.refresh_tokens

    set
        revoked_at = now(),
        replaced_by_token = @newToken

    where token = @oldToken
    """;

        command.Parameters.AddWithValue("oldToken", oldToken);

        command.Parameters.AddWithValue("newToken", newToken);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RevokeAll(Guid userId)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
    update xpbro.refresh_tokens

    set revoked_at = now()

    where user_id = @userId
    and revoked_at is null
    """;

        command.Parameters.AddWithValue("userId", userId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteExpired()
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
    delete from xpbro.refresh_tokens

    where expires_at < now()
    """;

        await command.ExecuteNonQueryAsync();
    }
}
