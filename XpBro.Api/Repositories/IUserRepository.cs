using XpBro.Api.Models;

namespace XpBro.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetById(Guid id);
    Task<User?> GetByEmail(string email);
    Task<List<User>> GetAll();
    Task Create(User user);
    Task Update(User user);
    Task Enable(Guid id);
    Task Disable(Guid id);
    Task ConfirmEmail(Guid id);
    Task UpdatePassword(Guid id, string passwordHash);

    Task<bool> AnyAdminsExist();
}
