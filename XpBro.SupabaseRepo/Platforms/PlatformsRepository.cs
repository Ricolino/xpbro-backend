using Microsoft.Extensions.Configuration;
using Npgsql;
using XpBro.Models.Platforms;
using XpBro.RepoInterfaces;

namespace XpBro.SupabaseRepo.Platforms;

public sealed class PlatformsRepository(IConfiguration configuration) : IPlatformsRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("Db")!;

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<List<Platform>> GetAllPlatforms()
    {
        List<Platform> platforms = [];

        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        select
            id,
            name,
            slug,
            auth_type,
            is_active,
            created_at
        from xpbro.platforms
        order by name
        """;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            platforms.Add(MapPlatform(reader));
        }

        return platforms;
    }

    public async Task<Platform> GetPlatformBySlug(string slug)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        select
            id,
            name,
            slug,
            auth_type,
            is_active,
            created_at
        from xpbro.platforms
        where slug = @slug
        """;

        command.Parameters.AddWithValue("slug", slug);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new KeyNotFoundException($"Platform with slug '{slug}' was not found.");
        }

        return MapPlatform(reader);
    }

    public async Task<Guid> GetPlatformIdBySlug(string slug)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        select id
        from xpbro.platforms
        where slug = @slug
        """;

        command.Parameters.AddWithValue("slug", slug);

        object? result = await command.ExecuteScalarAsync();

        if (result is not Guid platformId)
        {
            throw new KeyNotFoundException($"Platform with slug '{slug}' was not found.");
        }

        return platformId;
    }

    public async Task<string> GetExternalUserName(Guid userId, Guid platformId)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        select external_username
        from xpbro.user_platforms
        where user_id = @userId
          and platform_id = @platformId
          and is_active = true
        """;

        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("platformId", platformId);

        object? result = await command.ExecuteScalarAsync();

        return result as string ?? string.Empty;
    }

    private static Platform MapPlatform(NpgsqlDataReader reader)
    {
        return new Platform
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1),
            Slug = reader.GetString(2),
            AuthType = reader.GetString(3),
            IsActive = reader.GetBoolean(4),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(5)
        };
    }
}
