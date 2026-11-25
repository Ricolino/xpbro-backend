using XpBro.Models.Domain;

namespace XpBro.RepoInterfaces;

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
    Task UpdateEmail(Guid id, string email);
    Task Delete(Guid id);


    Task<bool> AnyAdminsExist();
}
