using XpBro.Models.Games;

namespace XpBro.RepoInterfaces;

public interface IGamesRepository
{
    Task Insert(Game game);
}
