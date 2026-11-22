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
    public async Task Series_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/levels/series");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Series_AreScopedPerUser()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);
        await alice.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow, "Semaglutide", 1.0m));

        var bobSeries = await bob.GetFromJsonAsync<LevelSeriesResponse>("/api/levels/series");

        bobSeries!.Series.Should().BeEmpty();
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

    [Fact]
    public async Task Series_ProjectsAQuarterOfTheHistoryPastNow()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var firstDose = DateTime.UtcNow.AddDays(-20);
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(firstDose, "Semaglutide", 1.0m));

        var series = await client.GetFromJsonAsync<LevelSeriesResponse>("/api/levels/series");

        series!.Times.Should().Contain(series.Now);
        var expectedEnd = series.Now + (series.Now - firstDose) * 0.25;
        series.Times[^1].Should().BeCloseTo(expectedEnd, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Series_ProjectionDecaysWithNoFurtherDoses()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        // Long enough ago that the dose is well past its absorption peak.
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow.AddDays(-20), "Semaglutide", 1.0m));

        var series = await client.GetFromJsonAsync<LevelSeriesResponse>("/api/levels/series");

        var nowIndex = series!.Times.ToList().IndexOf(series.Now);
        var amounts = series.Series.Single().AmountsMg;
        var projected = amounts.Skip(nowIndex).ToList();
        projected.Count.Should().BeGreaterThan(1);
        projected.Should().BeInDescendingOrder();
        projected[^1].Should().BeLessThan(projected[0]);
    }

    [Fact]
    public async Task NextDoses_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/levels/next-doses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NextDoses_WithNoDoses_ReturnsEmpty()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var response = await client.GetFromJsonAsync<NextDosesResponse>("/api/levels/next-doses");

        response!.NextDoses.Should().BeEmpty();
    }

    [Fact]
    public async Task NextDoses_UsesTheLatestDosePlusTheInterval()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var latest = DateTime.UtcNow.AddDays(-2);
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(latest.AddDays(-5), "Tirzepatide", 2.5m));
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(latest, "Tirzepatide", 5.0m));

        var response = await client.GetFromJsonAsync<NextDosesResponse>("/api/levels/next-doses");

        var next = response!.NextDoses.Should().ContainSingle().Subject;
        next.MedicationName.Should().Be("Tirzepatide");
        next.IntervalDays.Should().Be(5);
        next.LastTakenAt.Should().BeCloseTo(latest, TimeSpan.FromMilliseconds(1));
        next.LastDoseAmountMg.Should().Be(5.0);
        next.NextDueAt.Should().BeCloseTo(latest.AddDays(5), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task NextDoses_LeavesOutMedicationsNoLongerBeingTaken()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        // Due 7 days after this, i.e. 33 days ago: more than 28 days overdue.
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow.AddDays(-40), "Semaglutide", 1.0m));
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow.AddDays(-1), "Retatrutide", 2.0m));

        var response = await client.GetFromJsonAsync<NextDosesResponse>("/api/levels/next-doses");

        response!.NextDoses.Select(n => n.MedicationName)
            .Should().BeEquivalentTo(new[] { "Retatrutide" });
    }

    [Fact]
    public async Task NextDoses_AreScopedPerUser()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);
        await alice.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(DateTime.UtcNow, "Semaglutide", 1.0m));

        var response = await bob.GetFromJsonAsync<NextDosesResponse>("/api/levels/next-doses");

        response!.NextDoses.Should().BeEmpty();
    }
}
