using Microsoft.Extensions.Configuration;
using Npgsql;
using XpBro.Models.Games;
using XpBro.RepoInterfaces;

namespace XpBro.SupabaseRepo.Games;

public sealed class GamesRepository(IConfiguration configuration) : IGamesRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("Db")!;

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task Insert(Game game)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        INSERT INTO xpbro.games
        (
            id,
            platform_id,
            external_game_id,
            name,
            image_url,
            metadata
        )
        VALUES
        (
            @id,
            @platformId,
            @externalGameId,
            @name,
            @imageUrl,
            @metadata
        )
        """;

        command.Parameters.AddWithValue("id", game.Id);
        command.Parameters.AddWithValue("platformId", game.PlatformId);
        command.Parameters.AddWithValue("externalGameId", game.ExternalGameId);
        command.Parameters.AddWithValue("name", game.Name);
        command.Parameters.AddWithValue("imageUrl", (object?)game.ImageUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("metadata", NpgsqlTypes.NpgsqlDbType.Jsonb, game.Metadata);

        await command.ExecuteNonQueryAsync();

        throw new NotImplementedException();
    }
}
