using Xpbro.Api.Models;

namespace XpBro.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmail(string email);

    //Task<User?> GetById(Guid id);

    //Task Create(User user);
}
