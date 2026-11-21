using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpBro.Api.Dtos;
using XpBro.Api.Models;
using XpBro.Api.Repositories;
using XpBro.Api.Services;

namespace XpBro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IUserRepository _users;

    private readonly IRoleRepository _roles;

    private readonly ICurrentUserService _currentUser;

    private readonly ISessionRepository _sessions;

    private readonly IPasswordService _passwords;

    public AccountController(
        IUserRepository users,
        IRoleRepository roles,
        ICurrentUserService currentUser,
        ISessionRepository sessions,
        IPasswordService passwords)
    {
        _users = users;
        _roles = roles;
        _currentUser = currentUser;
        _sessions = sessions;
        _passwords = passwords;
    }

    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me()
    {
        Guid userId = _currentUser.UserId();

        User? user = await _users.GetById(userId);

        if (user == null)
        {
            return NotFound();
        }

        List<string> roles = await _roles.GetRoles(userId);

        return Ok(new MeResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            IsActive = user.IsActive,
            Roles = roles
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        Guid userId = _currentUser.UserId();

        User? user = await _users.GetById(userId);

        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        await _users.Update(user);

        return Ok();
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        Guid userId = _currentUser.UserId();

        User? user = await _users.GetById(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return BadRequest("Google account.");
        }

        bool valid = _passwords.Verify(request.CurrentPassword, user.PasswordHash);

        if (!valid)
        {
            return BadRequest("Current password invalid.");
        }

        string hash = _passwords.Hash(request.NewPassword);

        await _users.UpdatePassword(user.Id, hash);

        return Ok();
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = _currentUser.UserId();

        var sessions = await _sessions.GetByUserId(userId);

        var result = sessions
            .OrderByDescending(x => x.LastSeenAt)
            .Select(x => new
            {
                x.Id,
                x.IpAddress,
                x.UserAgent,
                x.CreatedAt,
                x.LastSeenAt,
                IsActive = x.RevokedAt is null
            });

        return Ok(result);
    }

    [HttpDelete("sessions/{id:guid}")]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        var userId = _currentUser.UserId();

        var session = await _sessions.Get(id);

        if (session is null)
        {
            return NotFound();
        }

        if (session.UserId != userId)
        {
            return Forbid();
        }

        await _sessions.Revoke(id);

        return NoContent();
    }
}
