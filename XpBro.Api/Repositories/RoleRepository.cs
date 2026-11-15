using Npgsql;
namespace XpBro.Api.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly string _connectionString;

    public RoleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Db")!;
    }

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<List<string>> GetRoles(Guid userId)
    {
        var roles = new List<string>();

        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        select
            r.name
        from xpbro.roles r
        inner join xpbro.user_roles ur
            on ur.role_id = r.id
        where ur.user_id = @userId
        order by r.name
        """;

        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            roles.Add(reader.GetString(0));
        }

        return roles;
    }

    public async Task<bool> HasRole(Guid userId, string roleName)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        select count(*)

        from xpbro.user_roles ur

        inner join xpbro.roles r
            on r.id = ur.role_id

        where ur.user_id = @userId
        and r.name = @roleName
        """;

        command.Parameters.AddWithValue("userId", userId);

        command.Parameters.AddWithValue("roleName", roleName);

        var count = (long)(await command.ExecuteScalarAsync())!;

        return count > 0;
    }

    public async Task AssignRole(Guid userId, string roleName)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        insert into xpbro.user_roles
        (
            user_id,
            role_id
        )

        select
            @userId,
            r.id
        from xpbro.roles r

        where r.name = @roleName

        on conflict
        do nothing
        """;

        command.Parameters.AddWithValue("userId", userId);

        command.Parameters.AddWithValue("roleName", roleName);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveRole(Guid userId, string roleName)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        delete from xpbro.user_roles

        where user_id = @userId
        and role_id =
        (
            select id
            from xpbro.roles
            where name = @roleName
        )
        """;

        command.Parameters.AddWithValue("userId", userId);

        command.Parameters.AddWithValue("roleName", roleName);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<string>> GetAllRoles()
    {
        var roles = new List<string>();

        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText =
        """
        select name
        from xpbro.roles
        order by name
        """;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            roles.Add(reader.GetString(0));
        }

        return roles;
    }
}
