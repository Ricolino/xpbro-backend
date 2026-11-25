namespace XpBro.Api.Dtos;

public sealed record ResetPasswordRequest
{
    public string NewPassword { get; set; } = "";
}
