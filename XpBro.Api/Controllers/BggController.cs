using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpBro.Api.Dtos;
using XpBro.Api.Services;
using XpBro.Models.Users;
using XpBro.RepoInterfaces;

namespace XpBro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/bgg")]
public sealed class BggController(
    IUserRepository users,
    ICurrentUserService currentUser,
    ILogger<BggController> logger) : ControllerBase
{
    [HttpGet("testconnection")]
    public async Task<ActionResult<TestBggConnectionResponse>> TestConnection()
    {
        Guid userId = currentUser.UserId();

        User? user = await users.GetById(userId);

        if (user == null)
        {
            return NotFound();
        }

        //TODO: read the BGG platform id from platforms table

        //TODO: Am userid si platform id-ul BGG si facem un query in user_platforms ca sa external_username-ul

        //TODO: Implement BGG API connection test logic here

        //curl -X GET "https://boardgamegeek.com/xmlapi2/collection?username=NameFromPreviousStep&own=1" - H "Accept: application/xml"


        return Ok(true);
    }
}
