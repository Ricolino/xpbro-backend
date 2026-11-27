using XpBro.Api.Seeders;
using XpBro.Api.Services;
using XpBro.RepoInterfaces;
using XpBro.SupabaseRepo.Games;
using XpBro.SupabaseRepo.Platforms;
using XpBro.SupabaseRepo.Repos;

namespace XpBro.Api;

public static class DependencyInjectionConfig

{

    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        //
        // SERVICES
        //
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IUserRepository, UserRepository>();

        //
        // REPOSITORIES
        //
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IGamesRepository, GamesRepository>();
        services.AddScoped<IPlatformsRepository, PlatformsRepository>();

        //
        // ADMIN SEEDER
        //
        services.AddScoped<IAdminSeederService, AdminSeederService>();

        return services;
    }
}
