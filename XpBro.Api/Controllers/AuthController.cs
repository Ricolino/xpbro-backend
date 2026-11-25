using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using XpBro.Api.Dtos;
using XpBro.Api.Services;
using XpBro.Models.Domain;
using XpBro.RepoInterfaces;

namespace XpBro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IUserRepository users,
    IRoleRepository roles,
    ITokenRepository tokens,
    ISessionRepository sessions,
    ICurrentUserService currentUser,
    IJwtService jwt,
    IRefreshTokenService refresh,
    IPasswordService passwords,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        User? existing = await users.GetByEmail(request.Email);

        if (existing != null)
        {
            return BadRequest("Email already exists.");
        }

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            PasswordHash = passwords.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false,
            IsActive = true
        };

        await users.Create(user);

        await roles.AssignRole(user.Id, "User");

        return Ok();
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        User? user = await users.GetByEmail(request.Email);

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

        bool passwordValid = passwords.Verify(request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            return Unauthorized();
        }

        string accessToken = jwt.Generate(user.Id, user.Email, await roles.GetRoles(user.Id));

        RefreshToken refreshToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refresh.Generate(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await tokens.Save(refreshToken);

        await sessions.Create(
        user.Id,
        refreshToken.Id,
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString());

        SetRefreshCookie(refreshToken.Token);

        return Ok(new LoginResponse { AccessToken = accessToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var cookie))
        {
            return Unauthorized();
        }

        RefreshToken? storedToken = await tokens.Get(cookie);

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

        User? user = await users.GetById(storedToken.UserId);

        if (user is null)
        {
            return Unauthorized();
        }

        string accessToken = jwt.Generate(
            user.Id,
            user.Email,
            await roles.GetRoles(user.Id));

        RefreshToken newRefreshToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refresh.Generate(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await tokens.Replace(storedToken.Token, newRefreshToken.Token);

        await tokens.Save(newRefreshToken);

        await sessions.RevokeByRefreshTokenId(storedToken.Id);

        await sessions.Create(
            user.Id,
            newRefreshToken.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        SetRefreshCookie(newRefreshToken.Token);

        return Ok(new { accessToken });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var token))
        {
            return Ok();
        }

        RefreshToken? storedToken = await tokens.Get(token);

        if (storedToken is not null)
        {
            await sessions.RevokeByRefreshTokenId(storedToken.Id);
        }
        await tokens.Revoke(token);

        RemoveRefreshCookie();

        return Ok();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        Guid userId = currentUser.UserId();

        await tokens.RevokeAll(userId);

        await sessions.RevokeAll(userId);

        RemoveRefreshCookie();

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

        User? user = await users.GetByEmail(email);

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

            await users.Create(user);

            await roles.AssignRole(user.Id, "User");
        }

        string accessToken = jwt.Generate(user.Id, user.Email, await roles.GetRoles(user.Id));

        RefreshToken refreshToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refresh.Generate(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        await tokens.Save(refreshToken);

        await sessions.Create(
            user.Id,
            refreshToken.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        SetRefreshCookie(refreshToken.Token);

        string? frontendUrl = configuration["Frontend:Url"];

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
