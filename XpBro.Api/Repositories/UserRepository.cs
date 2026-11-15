using Npgsql;
using Xpbro.Api.Models;

namespace XpBro.Api.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Db")!;
    }

    public async Task<User?> GetByEmail(
    string email)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
    select
        id,
        email,
        password_hash,
        google_id,
        first_name,
        last_name,
        email_confirmed,
        is_active
    from xpbro.users
    where email = @email
    """;

        command.Parameters.AddWithValue("email", email);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new User
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            PasswordHash = reader.IsDBNull(2) ? null : reader.GetString(2),
            GoogleId = reader.IsDBNull(3) ? null : reader.GetString(3),
            FirstName = reader.GetString(4),
            LastName = reader.GetString(5),
            EmailConfirmed = reader.GetBoolean(6),
            IsActive = reader.GetBoolean(7)
        };
    }
}
