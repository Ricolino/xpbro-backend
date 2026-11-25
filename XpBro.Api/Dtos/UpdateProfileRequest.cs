namespace XpBro.Api.Dtos;

public sealed record UpdateProfileRequest
{
    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";
}
