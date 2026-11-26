using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using AwesomeAssertions;

namespace Tests.Integration;

public class VialsApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();
    private static readonly DateOnly Started = new(2025, 3, 14);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetVials_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/vials");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutVial_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.PutAsJsonAsync("/api/vials/Semaglutide", new UpdateVialRequest(5m, 2m, Started));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetVials_WithNoneSaved_ReturnsAnEmptyList()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var vials = await client.GetFromJsonAsync<List<VialRecord>>("/api/vials");

        vials.Should().BeEmpty();
    }

    [Fact]
    public async Task PutThenGet_RoundTripsTheVial_UsingTheStoredMedicationName()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var put = await client.PutAsJsonAsync("/api/vials/tirzepatide", new UpdateVialRequest(10m, 2.5m, Started));

        put.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var vials = await client.GetFromJsonAsync<List<VialRecord>>("/api/vials");
        vials.Should().ContainSingle();
        vials![0].MedicationName.Should().Be("Tirzepatide");
        vials[0].UserId.Should().Be(ApiTestFactory.AliceUserId);
        vials[0].InitialAmountMg.Should().Be(10m);
        vials[0].InitialVolumeMl.Should().Be(2.5m);
        vials[0].StartedOn.Should().Be(Started);
    }

    [Fact]
    public async Task Put_Again_ReplacesTheVialRatherThanAddingAnother()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PutAsJsonAsync("/api/vials/Semaglutide", new UpdateVialRequest(5m, 2m, Started));

        await client.PutAsJsonAsync("/api/vials/Semaglutide", new UpdateVialRequest(10m, 3m, new DateOnly(2025, 4, 1)));

        var vials = await client.GetFromJsonAsync<List<VialRecord>>("/api/vials");
        vials.Should().ContainSingle();
        vials![0].InitialAmountMg.Should().Be(10m);
        vials[0].InitialVolumeMl.Should().Be(3m);
        vials[0].StartedOn.Should().Be(new DateOnly(2025, 4, 1));
    }

    [Fact]
    public async Task GetVials_OnlyReturnsTheCallersVials()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);
        await alice.PutAsJsonAsync("/api/vials/Semaglutide", new UpdateVialRequest(5m, 2m, Started));
        await bob.PutAsJsonAsync("/api/vials/Semaglutide", new UpdateVialRequest(3m, 1m, Started));

        var aliceVials = await alice.GetFromJsonAsync<List<VialRecord>>("/api/vials");
        var bobVials = await bob.GetFromJsonAsync<List<VialRecord>>("/api/vials");

        aliceVials.Should().ContainSingle(v => v.UserId == ApiTestFactory.AliceUserId && v.InitialAmountMg == 5m);
        bobVials.Should().ContainSingle(v => v.UserId == ApiTestFactory.BobUserId && v.InitialAmountMg == 3m);
    }

    [Fact]
    public async Task Put_WithAnUnknownMedication_ReturnsBadRequestAndSavesNothing()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var put = await client.PutAsJsonAsync("/api/vials/Ozempic", new UpdateVialRequest(5m, 2m, Started));

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetFromJsonAsync<List<VialRecord>>("/api/vials")).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(5, 0)]
    [InlineData(-1, 2)]
    [InlineData(5, -1)]
    public async Task Put_WithAnAmountThatIsNotPositive_ReturnsBadRequestAndSavesNothing(decimal amountMg, decimal volumeMl)
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var put = await client.PutAsJsonAsync("/api/vials/Semaglutide", new UpdateVialRequest(amountMg, volumeMl, Started));

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetFromJsonAsync<List<VialRecord>>("/api/vials")).Should().BeEmpty();
    }

    [Fact]
    public async Task Put_WithoutAStartDate_ReturnsBadRequestAndSavesNothing()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var put = await client.PutAsJsonAsync("/api/vials/Semaglutide", new { initialAmountMg = 5m, initialVolumeMl = 2m });

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetFromJsonAsync<List<VialRecord>>("/api/vials")).Should().BeEmpty();
    }
}
