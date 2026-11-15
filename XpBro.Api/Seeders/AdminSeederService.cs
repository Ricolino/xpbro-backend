using XpBro.Api.Models;
using XpBro.Api.Repositories;
using XpBro.Api.Services;

namespace XpBro.Api.Seeders;

public sealed class AdminSeederService : IAdminSeederService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordService _passwords;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSeederService> _logger;

    public AdminSeederService(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordService passwords,
        IConfiguration configuration,
        ILogger<AdminSeederService> logger)
    {
        _users = users;
        _roles = roles;
        _passwords = passwords;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Seed()
    {
        const string adminEmail = "admin@xpbro.com";

        if(await _users.AnyAdminsExist())
        {
            return;
        }

        User admin = new()
        {
            Id = Guid.NewGuid(),
            Email = _configuration["SeedAdmin:Email"]!,
            PasswordHash = _passwords.Hash(_configuration["SeedAdmin:Password"]!),
            FirstName = _configuration["SeedAdmin:FirstName"]!,
            LastName = _configuration["SeedAdmin:LastName"]!,
            EmailConfirmed = true,
            IsActive = true
        };

        await _users.Create(admin);

        await _roles.AssignRole(admin.Id, "Admin");

        _logger.LogInformation("Default admin created.");
    }
}
