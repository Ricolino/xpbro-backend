using Microsoft.AspNetCore.Mvc;
using Xpbro.Api.Models;
using XpBro.Api.Dtos;
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
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenService _refresh;
    private readonly ITokenRepository _tokens;

    public AuthController(IUserRepository users, PasswordService passwords, IJwtService jwt)
    {
        _users = users;
        _passwords = passwords;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        User? existing = await _users.GetByEmail(request.Email);

        if (existing != null)
        {
            return BadRequest("Email already exists");
        }

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwords.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        await _users.Create(user);

        await _roles.AssignRole(user.Id, "User");

        return Ok();
    }

    [HttpPost("login")]
    public async Task<
    ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        User? user = await _users.GetByEmail(request.Email);

        if (user == null)
        {
            return Unauthorized();
        }

        if (!_passwords.Verify(request.Password, user.PasswordHash!))
        {
            return Unauthorized();
        }

        List<string> roles = await _roles.GetRoles(user.Id);

        var accessToken = _jwt.Generate(user.Id, user.Email, roles);

        var refreshToken = _refresh.Generate();

        await _tokens.Save(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    //todo [HttpPost("refresh")]


    //todo 
    //[Authorize]
    //[HttpPost("logout")]
    //public async Task<IActionResult> Logout(LogoutRequest request)
    //{
    //    await _tokens.Revoke(request.RefreshToken);
    //    return Ok();
    //}


}
