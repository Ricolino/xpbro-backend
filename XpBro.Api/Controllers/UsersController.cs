using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpBro.Api.Dtos;
using XpBro.Api.Models;
using XpBro.Api.Repositories;
using XpBro.Api.Services;

namespace XpBro.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordService _passwords;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordService passwords,
        ICurrentUserService currentUserService)
    {
        _users = users;
        _roles = roles;
        _passwords = passwords;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserListItemResponse>>> GetUsers()
    {
        List<User> users = await _users.GetAll();

        Dictionary<Guid, List<string>> roleMap = await _roles.GetRolesForUsers(users.Select(x => x.Id));

        List<UserListItemResponse> result = users.ConvertAll(
                user => new UserListItemResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roleMap.TryGetValue(user.Id, out var roles) ? roles : []
                });

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserListItemResponse>> GetUser(Guid id)
    {
        User? user = await _users.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        List<string> roles = await _roles.GetRoles(user.Id);

        return Ok(new UserListItemResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
    {
        User? user = await _users.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = request.FirstName.Trim();

        user.LastName = request.LastName.Trim();

        await _users.Update(user);

        await _users.UpdateEmail(user.Id, request.Email.Trim());

        return Ok();
    }

    [HttpPut("{id:guid}/enable")]
    public async Task<IActionResult> EnableUser(Guid id)
    {
        await _users.Enable(id);

        return Ok();
    }

    [HttpPut("{id:guid}/disable")]
    public async Task<IActionResult> DisableUser(Guid id)
    {
        await _users.Disable(id);

        return Ok();
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole(Guid id,
    AssignRoleRequest request)
    {
        await _roles.AssignRole(id, request.Role);

        return Ok();
    }

    [HttpDelete(
    "{id:guid}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(Guid id, string role)
    {
        await _roles.RemoveRole(id, role);

        return Ok();
    }

    [HttpPut(
    "{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request)
    {
        User? user = await _users.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        string hash = _passwords.Hash(request.NewPassword);

        await _users.UpdatePassword(id, hash);

        return Ok();
    }

    [HttpGet("{id:guid}/roles")]
    public async Task<ActionResult<List<string>>> GetRoles(Guid id)
    {
        List<string> roles = await _roles.GetRoles(id);

        return Ok(roles);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        User? user = await _users.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        if (id == _currentUserService.UserId())
        {
            return BadRequest("Cannot delete yourself.");
        }

        bool isAdmin = await _roles.HasRole(id, "Admin");

        if (isAdmin && await _roles.CountUsersInRole("Admin") == 1)
        {
            return BadRequest("Last admin cannot be deleted.");
        }

        await _users.Delete(id);

        return NoContent();
    }
}
