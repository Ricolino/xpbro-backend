using XpBro.Models.Platforms;

namespace XpBro.RepoInterfaces;

public interface IPlatformsRepository
{
    Task<List<Platform>> GetAllPlatforms();

    Task<Platform> GetPlatformBySlug(string slug);

    Task<Guid> GetPlatformIdBySlug(string slug);
}
