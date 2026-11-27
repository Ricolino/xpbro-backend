using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpBro.Api.Dtos;
using XpBro.Api.Services;
using XpBro.Models.Users;
using XpBro.RepoInterfaces;

namespace XpBro.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/users")]
public sealed class UsersController(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordService passwords,
    ICurrentUserService currentUserService,
    ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserListItemResponse>>> GetUsers()
    {
        List<User> allUsers = await users.GetAll();

        Dictionary<Guid, List<string>> roleMap = await roles.GetRolesForUsers(allUsers.Select(x => x.Id));

        List<UserListItemResponse> result = allUsers.ConvertAll(
                user => new UserListItemResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roleMap.TryGetValue(user.Id, out var userRoles) ? userRoles : []
                });

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserListItemResponse>> GetUser(Guid id)
    {
        User? user = await users.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserListItemResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            Roles = await roles.GetRoles(user.Id)
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
    {
        User? user = await users.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = request.FirstName.Trim();

        user.LastName = request.LastName.Trim();

        await users.Update(user);

        await users.UpdateEmail(user.Id, request.Email.Trim());

        return Ok();
    }

    [HttpPut("{id:guid}/enable")]
    public async Task<IActionResult> EnableUser(Guid id)
    {
        await users.Enable(id);

        return Ok();
    }

    [HttpPut("{id:guid}/disable")]
    public async Task<IActionResult> DisableUser(Guid id)
    {
        if (await roles.HasRole(id, "Admin"))
        {
            return BadRequest("Cannot disable admin user.");
        }

        await users.Disable(id);

        return Ok();
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest request)
    {
        await roles.AssignRole(id, request.Role);

        return Ok();
    }

    [HttpDelete(
    "{id:guid}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(Guid id, string role)
    {
        await roles.RemoveRole(id, role);

        return Ok();
    }

    [HttpPut(
    "{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request)
    {
        User? user = await users.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        string hash = passwords.Hash(request.NewPassword);

        await users.UpdatePassword(id, hash);

        return Ok();
    }

    [HttpGet("{id:guid}/roles")]
    public async Task<ActionResult<List<string>>> GetRoles(Guid id)
    {
        return Ok(await roles.GetRoles(id));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        User? user = await users.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        if (id == currentUserService.UserId())
        {
            return BadRequest("Cannot delete yourself.");
        }

        bool isAdmin = await roles.HasRole(id, "Admin");

        if (isAdmin && await roles.CountUsersInRole("Admin") == 1)
        {
            return BadRequest("Last admin cannot be deleted.");
        }

        await users.Delete(id);

        return NoContent();
    }
}
