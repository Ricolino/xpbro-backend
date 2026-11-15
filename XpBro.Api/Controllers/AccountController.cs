using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xpbro.Api.Models;

namespace XpBro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    //todo
    //[HttpGet("me")]
    //public IActionResult GetMe()
    //{
    //    var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    //    var user =    await _users.GetById(        Guid.Parse(userId));
    //    return Ok(new
    //    {
    //        user.Id,
    //        user.Email,
    //        user.FirstName,
    //        user.LastName
    //    });
    //}
}
