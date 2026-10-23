using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Levels;
using Api.Controllers.Models;
using AwesomeAssertions;

namespace Tests.Integration;

public class LevelsApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Levels_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/levels");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Levels_WithNoDoses_ReturnsEmpty()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var levels = await client.GetFromJsonAsync<LevelsResponse>("/api/levels");

        levels!.Levels.Should().BeEmpty();
    }

    [Fact]
    public async Task Levels_ReportsOnlyMedicationsTheUserHasDosed()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow, "Semaglutide", 1.0m));
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow, "Tirzepatide", 5.0m));

        var levels = await client.GetFromJsonAsync<LevelsResponse>("/api/levels");

        // Retatrutide was never dosed, so it is not reported.
        levels!.Levels.Select(l => l.MedicationName)
            .Should().BeEquivalentTo(new[] { "Semaglutide", "Tirzepatide" });
    }

    [Fact]
    public async Task Levels_AreScopedPerUser()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);
        await alice.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow, "Semaglutide", 1.0m));

        var bobLevels = await bob.GetFromJsonAsync<LevelsResponse>("/api/levels");

        bobLevels!.Levels.Should().BeEmpty();
    }

    [Fact]
    public async Task Series_WithNoDoses_ReturnsEmpty()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var series = await client.GetFromJsonAsync<LevelSeriesResponse>("/api/levels/series");

        series!.Times.Should().BeEmpty();
        series.Series.Should().BeEmpty();
    }

    [Fact]
    public async Task Series_ReturnsAlignedSeriesForDosedMedications()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow.AddDays(-7), "Semaglutide", 1.0m));
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow, "Semaglutide", 1.0m));
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow, "Tirzepatide", 5.0m));

        var series = await client.GetFromJsonAsync<LevelSeriesResponse>("/api/levels/series");

        series!.Times.Should().NotBeEmpty();
        series.Series.Select(s => s.MedicationName)
            .Should().BeEquivalentTo(new[] { "Semaglutide", "Tirzepatide" });
        // Every series aligns element-for-element with the shared time axis.
        series.Series.Should().OnlyContain(s => s.AmountsMg.Count == series.Times.Count);
    }
}
