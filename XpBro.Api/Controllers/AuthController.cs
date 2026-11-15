using Microsoft.AspNetCore.Mvc;
using Xpbro.Api.Services;
using XpBro.Api.Repositories;
using XpBro.Api.Services;

namespace XpBro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;

    private readonly PasswordService _passwords;

    private readonly IJwtService _jwt;

    public AuthController(IUserRepository users, PasswordService passwords, IJwtService jwt)
    {
        _users = users;
        _passwords = passwords;
        _jwt = jwt;
    }
}
