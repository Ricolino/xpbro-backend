using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Xml;
using System.Xml.Linq;
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
    IPlatformsRepository platforms,
    ILogger<BggController> logger,
    HttpClient httpClient) : ControllerBase
{
    private readonly HttpClient _httpClient = httpClient;

    [HttpGet("connection-test")]
    public async Task<ActionResult<TestBggConnectionResponse>> TestConnection()
    {
        Guid userId = currentUser.UserId();

        User? user = await users.GetById(userId);

        if (user == null)
        {
            return NotFound();
        }

        TestBggConnectionResponse response = new();

        Guid bggId = await platforms.GetPlatformIdBySlug("bgg");

        string externalUserName = await platforms.GetExternalUserName(userId, bggId);

        if (string.IsNullOrWhiteSpace(externalUserName))
        {
            return Ok(response);
        }

        string requestUri = $"https://boardgamegeek.com/xmlapi2/collection?username={Uri.EscapeDataString(externalUserName)}&own=1";

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/xml");

            using HttpResponseMessage bggResponse = await _httpClient.SendAsync(request);

            response.ApiReachable = true;
            response.UserExists = bggResponse.StatusCode != HttpStatusCode.NotFound &&
                                  (int)bggResponse.StatusCode < 500;

            if (bggResponse.StatusCode != HttpStatusCode.OK)
            {
                return Ok(response);
            }

            string content = await bggResponse.Content.ReadAsStringAsync();
            XDocument document = XDocument.Parse(content);
            XElement? collection = document.Root;

            if (collection?.Name.LocalName != "items")
            {
                return Ok(response);
            }

            response.CollectionAccessible = true;
            response.CollectionCount = (int?)collection.Attribute("total") ??
                                       collection.Elements("item").Count();
            response.ImportedGamesCount = collection.Elements("item").Count();
            response.Success = response.UserExists && response.CollectionAccessible;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Unable to reach BoardGameGeek for user {BggUserName}", externalUserName);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogWarning(exception, "The BoardGameGeek request timed out for user {BggUserName}", externalUserName);
        }
        catch (XmlException exception)
        {
            logger.LogWarning(exception, "BoardGameGeek returned invalid XML for user {BggUserName}", externalUserName);
        }

        return Ok(response);
    }
}
