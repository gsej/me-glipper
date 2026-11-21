using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using AwesomeAssertions;

namespace Tests.Integration;

public class HealthApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Healthz_RequiresNoApiKey()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/healthz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Authenticating_SeedsAProfileRow()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        // The auth filter runs before the action, so this single authenticated request
        // both seeds alice's profile row and returns it (via the backup snapshot).
        var backup = await client.GetFromJsonAsync<Backup>("/api/backup");
        backup!.Users!.Should().ContainSingle(u => u.UserId == ApiTestFactory.AliceUserId);
    }
}
