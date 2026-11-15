namespace XpBro.Api.Services;

public interface IJwtService
{
    string Generate(Guid userId, string email, IEnumerable<string> roles);
}
