using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Tests.Integration;

public class DoseApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetDoses_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/doses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDoses_WithUnknownApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey("not-a-real-key");

        var response = await client.GetAsync("/api/doses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostThenGet_RoundTripsTheRecord()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var takenAt = new DateTime(2025, 1, 15, 8, 30, 0, DateTimeKind.Utc);

        var post = await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(takenAt, "Semaglutide", 1.0m));

        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var collection = await client.GetFromJsonAsync<DosesCollection>("/api/doses");

        using var _ = new AssertionScope();
        var records = collection!.DoseRecords!.ToList();
        records.Should().HaveCount(1);
        records[0].MedicationName.Should().Be("Semaglutide");
        records[0].DoseAmountMg.Should().Be(1.0m);
        records[0].TakenAt.Should().Be(takenAt);
        records[0].UserId.Should().Be(ApiTestFactory.AliceUserId);
        records[0].Deleted.Should().BeFalse();
    }

    [Fact]
    public async Task Doses_AreScopedPerUser()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);

        await alice.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m));

        var bobCollection = await bob.GetFromJsonAsync<DosesCollection>("/api/doses");

        bobCollection!.DoseRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ForAnotherUsersRecord_ReturnsNotFound()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);

        await alice.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m));
        var aliceRecord = (await alice.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();

        var bobResponse = await bob.GetAsync($"/api/dose/{aliceRecord.DoseId}");

        bobResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_SoftDeletes_SoTheRecordDisappearsFromReads()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m));
        var record = (await client.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();

        var delete = await client.DeleteAsync($"/api/dose/{record.DoseId}");

        using var _ = new AssertionScope();
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getById = await client.GetAsync($"/api/dose/{record.DoseId}");
        getById.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var collection = await client.GetFromJsonAsync<DosesCollection>("/api/doses");
        collection!.DoseRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task PostThenGet_RoundTripsTheNotes()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m, "first dose of the week"));

        var record = (await client.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();

        record.Notes.Should().Be("first dose of the week");
    }

    [Fact]
    public async Task PostWithoutNotes_YieldsNoNotes()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m));

        var record = (await client.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();

        record.Notes.Should().BeNull();
    }

    [Fact]
    public async Task Put_UpdatesTheNotes()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m));
        var record = (await client.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();

        var put = await client.PutAsJsonAsync($"/api/dose/{record.DoseId}",
            new UpdateDoseRequest("added later"));

        using var _ = new AssertionScope();
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updated = (await client.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();
        updated.Notes.Should().Be("added later");
    }

    [Fact]
    public async Task Put_ForAnotherUsersRecord_ReturnsNotFoundAndDoesNotChangeIt()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);
        await alice.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m, "alice's note"));
        var aliceRecord = (await alice.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();

        var bobPut = await bob.PutAsJsonAsync($"/api/dose/{aliceRecord.DoseId}",
            new UpdateDoseRequest("bob's meddling"));

        using var _ = new AssertionScope();
        bobPut.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var stillAlices = (await alice.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();
        stillAlices.Notes.Should().Be("alice's note");
    }

    [Fact]
    public async Task Put_ForUnknownRecord_ReturnsNotFound()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var put = await client.PutAsJsonAsync($"/api/dose/{Guid.NewGuid()}",
            new UpdateDoseRequest("nope"));

        put.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithOverlongNotes_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var response = await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m, new string('x', 201)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_WithOverlongNotes_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/dose",
            new CreateDoseRequest(new DateTime(2025, 1, 15), "Semaglutide", 1.0m));
        var record = (await client.GetFromJsonAsync<DosesCollection>("/api/doses"))!
            .DoseRecords!.Single();

        var put = await client.PutAsJsonAsync($"/api/dose/{record.DoseId}",
            new UpdateDoseRequest(new string('x', 201)));

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
