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
    private readonly ISessionRepository _sessions;
    private readonly ICurrentUserService _currentUser;
    private readonly IJwtService _jwt;
    private readonly IRefreshTokenService _refresh;
    private readonly IPasswordService _passwords;
    private readonly IConfiguration _configuration;

    public AuthController(
        IUserRepository users,
        IRoleRepository roles,
        ITokenRepository tokens,
        ISessionRepository sessions,
        ICurrentUserService currentUser,
        IJwtService jwt,
        IRefreshTokenService refresh,
        IPasswordService passwords,
        IConfiguration configuration)
    {
        _users = users;
        _roles = roles;
        _tokens = tokens;
        _sessions = sessions;
        _currentUser = currentUser;
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

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _refresh.Generate(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await _tokens.Save(refreshToken);

        await _sessions.Create(
        user.Id,
        refreshToken.Id,
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString());

        SetRefreshCookie(refreshToken.Token);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var cookie))
        {
            return Unauthorized();
        }

        var storedToken = await _tokens.Get(cookie);

        if (storedToken is null)
        {
            return Unauthorized();
        }

        if (storedToken.RevokedAt is not null)
        {
            return Unauthorized();
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized();
        }

        var user = await _users.GetById(storedToken.UserId);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _roles.GetRoles(user.Id);

        var accessToken = _jwt.Generate(
            user.Id,
            user.Email,
            roles);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _refresh.Generate(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await _tokens.Replace(
            storedToken.Token,
            newRefreshToken.Token);

        await _tokens.Save(newRefreshToken);

        await _sessions.RevokeByRefreshTokenId(storedToken.Id);

        await _sessions.Create(
            user.Id,
            newRefreshToken.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        SetRefreshCookie(newRefreshToken.Token);

        return Ok(new
        {
            accessToken
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var token))
        {
            return Ok();
        }

        RefreshToken? storedToken = await _tokens.Get(token);

        if (storedToken is not null)
        {
            await _sessions.RevokeByRefreshTokenId(storedToken.Id);
        }
        await _tokens.Revoke(token);

        RemoveRefreshCookie();

        return Ok();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        Guid userId = _currentUser.UserId();

        await _tokens.RevokeAll(userId);

        await _sessions.RevokeAll(userId);

        RemoveRefreshCookie();

        return Ok();
    }

    [HttpGet("google/login")]
    public IActionResult GoogleLogin()
    {
        AuthenticationProperties properties = new()
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        AuthenticateResult authenticate = await HttpContext.AuthenticateAsync("ExternalCookie");

        if (!authenticate.Succeeded || authenticate.Principal == null)
        {
            return Unauthorized();
        }

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

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _refresh.Generate(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await _tokens.Save(refreshToken);

        await _sessions.Create(
            user.Id,
            refreshToken.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        SetRefreshCookie(refreshToken.Token);

        string? frontendUrl = _configuration["Frontend:Url"];

        return Redirect($"{frontendUrl}/auth/callback?token={accessToken}");
    }

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(
            "refreshToken",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
    }

    private void RemoveRefreshCookie()
    {
        Response.Cookies.Delete(
            "refreshToken",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
    }
}
