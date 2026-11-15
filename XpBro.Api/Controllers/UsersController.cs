using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace XpBro.Api.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    //todo
    //[HttpGet]
    //public IActionResult GetUsers()
    //{
    //    // Implementation for getting users
    //    return Ok();
    //}

    //todo
    //[HttpPut("{id}/disable")]

    //todo
    //[HttpPut("{id}/enable")]

    //todo grant roles
    //[HttpPost("{id}/roles")]


}
