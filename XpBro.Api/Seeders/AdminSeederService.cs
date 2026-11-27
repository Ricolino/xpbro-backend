using XpBro.Api.Services;
using XpBro.Models.Users;
using XpBro.RepoInterfaces;

namespace XpBro.Api.Seeders;

public sealed class AdminSeederService(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordService passwords,
    IConfiguration configuration,
    ILogger<AdminSeederService> logger) : IAdminSeederService
{
    public async Task Seed()
    {
        if (await users.AnyAdminsExist())
        {
            return;
        }

        User admin = new()
        {
            Id = Guid.NewGuid(),
            Email = configuration["SeedAdmin:Email"]!,
            PasswordHash = passwords.Hash(configuration["SeedAdmin:Password"]!),
            FirstName = configuration["SeedAdmin:FirstName"]!,
            LastName = configuration["SeedAdmin:LastName"]!,
            EmailConfirmed = true,
            IsActive = true
        };

        await users.Create(admin);

        await roles.AssignRole(admin.Id, "Admin");

        logger.LogInformation("Default admin created.");
    }
}
