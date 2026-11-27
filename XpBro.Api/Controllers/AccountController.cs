using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpBro.Api.Dtos;
using XpBro.Api.Services;
using XpBro.Models.Auth;
using XpBro.Models.Users;
using XpBro.RepoInterfaces;

namespace XpBro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/account")]
public sealed class AccountController(
    IUserRepository users,
    IRoleRepository roles,
    ICurrentUserService currentUser,
    ISessionRepository sessions,
    IPasswordService passwords,
    ILogger<AccountController> logger) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me()
    {
        Guid userId = currentUser.UserId();

        User? user = await users.GetById(userId);

        if (user == null)
        {
            return NotFound();
        }

        List<string> myRoles = await roles.GetRoles(userId);

        return Ok(new MeResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            IsActive = user.IsActive,
            Roles = myRoles
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        Guid userId = currentUser.UserId();

        User? user = await users.GetById(userId);

        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        await users.Update(user);

        return Ok();
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        Guid userId = currentUser.UserId();

        User? user = await users.GetById(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return BadRequest("Google account.");
        }

        bool valid = passwords.Verify(request.CurrentPassword, user.PasswordHash);

        if (!valid)
        {
            return BadRequest("Current password invalid.");
        }

        string hash = passwords.Hash(request.NewPassword);

        await users.UpdatePassword(user.Id, hash);

        return Ok();
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        Guid userId = currentUser.UserId();

        IEnumerable<UserSession> userSessions = await sessions.GetByUserId(userId);

        var result = userSessions
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
        Guid userId = currentUser.UserId();

        UserSession? session = await sessions.Get(id);

        if (session is null)
        {
            return NotFound();
        }

        if (session.UserId != userId)
        {
            return Forbid();
        }

        await sessions.Revoke(id);

        return NoContent();
    }
}
