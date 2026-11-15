namespace XpBro.Api.Dtos;

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";

    public string NewPassword { get; set; } = "";
}
