namespace XpBro.Api.Dtos;

public sealed record ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";

    public string NewPassword { get; set; } = "";
}
