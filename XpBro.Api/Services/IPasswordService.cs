namespace XpBro.Api.Services;

public interface IPasswordService
{
    string Hash(string password);
    public bool Verify(string password, string hash);
}
