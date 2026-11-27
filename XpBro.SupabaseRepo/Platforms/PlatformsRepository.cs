using XpBro.Models.Platforms;
using XpBro.RepoInterfaces;

namespace XpBro.SupabaseRepo.Platforms;

public sealed class PlatformsRepository : IPlatformsRepository
{
    public async Task<List<Platform>> GetAllPlatforms()
    {
        throw new NotImplementedException();
    }

    public async Task<Platform> GetPlatformBySlug(string slug)
    {
        throw new NotImplementedException();
    }

    public async Task<Guid> GetPlatformIdBySlug(string slug)
    {
        throw new NotImplementedException();
    }
}
