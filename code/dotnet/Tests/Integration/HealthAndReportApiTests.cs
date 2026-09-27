using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using Api.Controllers.Report;
using Api.Filters;
using AwesomeAssertions;

namespace Tests.Integration;

public class HealthAndReportApiTests : IDisposable
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
    public async Task Report_ReturnsAnEntryForThePostedWeight()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        // The report looks up the user's height, so the user row must exist.
        await client.PostAsJsonAsync("/api/backup/restore",
            new Backup(new[] { new User(ApiTestFactory.AliceUserId, 180) }, Array.Empty<WeightRecord>()));
        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 90m));

        var report = await client.GetFromJsonAsync<WeightReport>("/api/report");

        report!.Entries.Should().ContainSingle();
        report.Entries[0].AverageWeight.Should().Be(90m);
    }

    [Fact]
    public async Task Authenticating_SeedsAProfileRowWithSentinelHeight()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        // The auth filter runs before the action, so this single authenticated request
        // both seeds alice's profile row and returns it (via the backup snapshot).
        var backup = await client.GetFromJsonAsync<Backup>("/api/backup");
        var alice = backup!.Users!.Should().ContainSingle(u => u.UserId == ApiTestFactory.AliceUserId).Subject;
        alice.heightInCm.Should().Be(ApiKeyAuthFilter.SeededHeightInCm);
    }

    [Fact]
    public async Task Report_ForUserWithNoWeights_ReturnsEmptyReport()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        // No weights posted — the report must not 500 on an empty date range.
        var response = await client.GetAsync("/api/report");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await response.Content.ReadFromJsonAsync<WeightReport>();
        report!.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Report_ForUserWithoutRestoredProfile_SucceedsUsingSeededHeight()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        // No restore performed — the profile row only exists because auth seeded it.
        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 90m));

        var response = await client.GetAsync("/api/report");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await response.Content.ReadFromJsonAsync<WeightReport>();
        report!.Entries.Should().ContainSingle();
        report.Entries[0].AverageWeight.Should().Be(90m);
    }
}
