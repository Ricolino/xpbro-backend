namespace XpBro.Api.Dtos;

public record TestBggConnectionResponse
{
    public bool Success { get; set; }

    public bool UserExists { get; set; }

    public bool CollectionAccessible { get; set; }

    public bool ApiReachable { get; set; }

    public int CollectionCount { get; set; }

    public int ImportedGamesCount { get; set; }
}
