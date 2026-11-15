namespace XpBro.Api.Repositories;

public interface IRoleRepository
{
    Task<List<string>> GetRoles(Guid userId);

    Task<bool> HasRole(Guid userId, string roleName);

    Task AssignRole(Guid userId, string roleName);

    Task RemoveRole(Guid userId, string roleName);

    Task<List<string>> GetAllRoles();
}
