using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using XpBro.Api.Dtos;
using XpBro.Api.Models;
using XpBro.Api.Repositories;
using XpBro.Api.Services;

namespace XpBro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ITokenRepository _tokens;
    private readonly IJwtService _jwt;
    private readonly IRefreshTokenService _refresh;
    private readonly PasswordService _passwords;
    private readonly IConfiguration _configuration;

    public AuthController(
        IUserRepository users,
        IRoleRepository roles,
        ITokenRepository tokens,
        IJwtService jwt,
        IRefreshTokenService refresh,
        PasswordService passwords,
        IConfiguration configuration)
    {
        _users = users;
        _roles = roles;
        _tokens = tokens;
        _jwt = jwt;
        _refresh = refresh;
        _passwords = passwords;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        User? existing = await _users.GetByEmail(request.Email);

        if (existing != null)
        {
            return BadRequest("Email already exists.");
        }

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            PasswordHash = _passwords.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false,
            IsActive = true
        };

        await _users.Create(user);

        await _roles.AssignRole(user.Id, "User");

        return Ok();
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        User? user = await _users.GetByEmail(request.Email);

        if (user == null)
        {
            return Unauthorized();
        }

        if (!user.IsActive)
        {
            return Unauthorized("User disabled.");
        }

        if (!user.EmailConfirmed)
        {
            return Unauthorized("Email not verified.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return Unauthorized();
        }

        bool passwordValid = _passwords.Verify(request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            return Unauthorized();
        }

        List<string> roles = await _roles.GetRoles(user.Id);

        string accessToken = _jwt.Generate(user.Id, user.Email, roles);

        string refreshToken = _refresh.Generate();

        await _tokens.Save(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        AppendRefreshCookie(refreshToken);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh()
    {
        string? cookie = Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(cookie))
        {
            return Unauthorized();
        }

        RefreshToken? token = await _tokens.Get(cookie);

        if (token == null)
        {
            return Unauthorized();
        }

        if (token.RevokedAt != null)
        {
            return Unauthorized();
        }

        if (token.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        User? user = await _users.GetById(token.UserId);

        if (user == null)
        {
            return Unauthorized();
        }

        List<string> roles = await _roles.GetRoles(user.Id);

        string accessToken = _jwt.Generate(user.Id, user.Email, roles);

        string newRefreshToken = _refresh.Generate();

        await _tokens.Replace(cookie, newRefreshToken);

        await _tokens.Save(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        AppendRefreshCookie(newRefreshToken);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        string? token = Request.Cookies["refreshToken"];

        if (!string.IsNullOrWhiteSpace(token))
        {
            await _tokens.Revoke(token);
        }

        Response.Cookies.Delete("refreshToken");

        return Ok();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        Guid userId = Guid.Parse(User.FindFirstValue("sub")!);

        await _tokens.RevokeAll(userId);

        Response.Cookies.Delete("refreshToken");

        return Ok();
    }

    [HttpGet("google/login")]
    public IActionResult GoogleLogin()
    {
        AuthenticationProperties properties = new()
        {
            RedirectUri = "/api/auth/google/callback"
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        AuthenticateResult authenticate = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        ClaimsPrincipal? principal = authenticate.Principal;

        if (principal == null)
        {
            return Unauthorized();
        }

        string? email = principal.FindFirstValue(ClaimTypes.Email);

        string? googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        string? firstName = principal.FindFirstValue(ClaimTypes.GivenName);

        string? lastName = principal.FindFirstValue(ClaimTypes.Surname);

        if (email == null)
        {
            return Unauthorized();
        }

        User? user = await _users.GetByEmail(email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                GoogleId = googleId,
                FirstName = firstName ?? "",
                LastName = lastName ?? "",
                EmailConfirmed = true,
                IsActive = true
            };

            await _users.Create(user);

            await _roles.AssignRole(user.Id, "User");
        }

        List<string> roles = await _roles.GetRoles(user.Id);

        string accessToken = _jwt.Generate(user.Id, user.Email, roles);

        string refreshToken = _refresh.Generate();

        await _tokens.Save(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        AppendRefreshCookie(refreshToken);

        string? frontendUrl = _configuration["Frontend:Url"];

        return Redirect($"{frontendUrl}/auth/callback?token={accessToken}");
    }

    private void AppendRefreshCookie(
    string refreshToken)
    {
        Response.Cookies.Append("refreshToken", refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
    }
}
