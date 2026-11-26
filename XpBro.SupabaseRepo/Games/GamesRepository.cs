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
        insert into xpbro.games
        (
            id,
            email,
            password_hash,
            google_id,
            first_name,
            last_name,
            email_confirmed,
            is_active
        )
        values
        (
            @id,
            @email,
            @passwordHash,
            @googleId,
            @firstName,
            @lastName,
            @emailConfirmed,
            @isActive
        )
        """;

        command.Parameters.AddWithValue("id", game.Id);

        command.Parameters.AddWithValue("email", game.Email);

        command.Parameters.AddWithValue("passwordHash", (object?)game.PasswordHash ?? DBNull.Value);

        command.Parameters.AddWithValue("googleId", (object?)game.GoogleId ?? DBNull.Value);

        command.Parameters.AddWithValue("firstName", game.FirstName);

        command.Parameters.AddWithValue("lastName", game.LastName);

        command.Parameters.AddWithValue("emailConfirmed", game.EmailConfirmed);

        command.Parameters.AddWithValue("isActive", game.IsActive);

        await command.ExecuteNonQueryAsync();


        throw new NotImplementedException();
    }
}
