using Microsoft.Extensions.Configuration;
using Npgsql;
using XpBro.Models.Domain;
using XpBro.RepoInterfaces;

namespace XpBro.SupabaseRepo.Repos;

public sealed class UserRepository(IConfiguration configuration) : IUserRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("Db")!;

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<User?> GetById(Guid id)
    {
        await using var connection = CreateConnection();

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
        where id = @id
        """;

        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapUser(reader);
    }

    public async Task<User?> GetByEmail(string email)
    {
        await using var connection = CreateConnection();

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

        return MapUser(reader);
    }

    public async Task<List<User>> GetAll()
    {
        List<User> users = [];

        await using var connection = CreateConnection();

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
        order by email
        """;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(MapUser(reader));
        }

        return users;
    }

    public async Task Create(User user)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        insert into xpbro.users
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

        command.Parameters.AddWithValue("id", user.Id);

        command.Parameters.AddWithValue("email", user.Email);

        command.Parameters.AddWithValue("passwordHash", (object?)user.PasswordHash ?? DBNull.Value);

        command.Parameters.AddWithValue("googleId", (object?)user.GoogleId ?? DBNull.Value);

        command.Parameters.AddWithValue("firstName", user.FirstName);

        command.Parameters.AddWithValue("lastName", user.LastName);

        command.Parameters.AddWithValue("emailConfirmed", user.EmailConfirmed);

        command.Parameters.AddWithValue("isActive", user.IsActive);

        await command.ExecuteNonQueryAsync();
    }

    public async Task Update(User user)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        update xpbro.users
        set
            first_name=@firstName,
            last_name=@lastName,
            is_active=@isActive
        where id=@id
        """;

        command.Parameters.AddWithValue("id", user.Id);

        command.Parameters.AddWithValue("firstName", user.FirstName);

        command.Parameters.AddWithValue("lastName", user.LastName);

        command.Parameters.AddWithValue("isActive", user.IsActive);

        await command.ExecuteNonQueryAsync();
    }

    public async Task Enable(Guid id)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        update xpbro.users
        set is_active = true
        where id = @id
        """;

        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task Disable(Guid id)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        update xpbro.users
        set is_active = false
        where id = @id
        """;

        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task ConfirmEmail(Guid id)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        update xpbro.users
        set email_confirmed = true
        where id = @id
        """;

        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdatePassword(Guid id, string passwordHash)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        update xpbro.users
        set password_hash=@passwordHash
        where id=@id
        """;

        command.Parameters.AddWithValue("id", id);

        command.Parameters.AddWithValue("passwordHash", passwordHash);

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateEmail(Guid id, string email)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        update xpbro.users
        set
            email = @email,
            email_confirmed = false
        where id = @id
        """;

        command.Parameters.AddWithValue("id", id);

        command.Parameters.AddWithValue("email", email);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> AnyAdminsExist()
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        select count(*)

        from xpbro.users u

        join xpbro.user_roles ur
            on ur.user_id = u.id

        join xpbro.roles r
            on r.id = ur.role_id

        where r.name = 'Admin'
        """;

        long result = (long)(await command.ExecuteScalarAsync())!;

        return result > 0;
    }

    private static User MapUser(NpgsqlDataReader reader)
    {
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

    public async Task Delete(Guid id)
    {
        await using NpgsqlConnection connection = CreateConnection();

        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();

        command.CommandText =
        """
        delete from xpbro.users
        where id = @id
        """;

        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }
}
