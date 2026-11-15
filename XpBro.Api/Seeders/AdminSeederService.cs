using XpBro.Api.Models;
using XpBro.Api.Services;

namespace XpBro.Api.Seeders;

public class AdminSeederService (IPasswordService passwordService): IAdminSeederService
{
    public async Task Seed()
    {
        // Implementation for seeding admin data
        //todo verify if admin exists, if not create one with default credentials

        //        select count(*)
        //from xpbro.users

        User admin = new()
        {
            Id = Guid.NewGuid(),
            Email = "admin@xpbro.com",
            PasswordHash = passwordService.Hash("Admin123!"),
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true
        };

        //todo save admin to database if not exists
        //todo attribute to mark this user as admin

    }
}
