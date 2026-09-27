using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Tests.Integration;

public class WeightApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetWeights_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/weights");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWeights_WithUnknownApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey("not-a-real-key");

        var response = await client.GetAsync("/api/weights");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostThenGet_RoundTripsTheRecord()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var post = await client.PostAsJsonAsync("/api/weight", new CreateWeightRecordRequest(date, 82.5m));

        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var collection = await client.GetFromJsonAsync<WeightsCollection>("/api/weights");

        using var _ = new AssertionScope();
        var records = collection!.WeightRecords!.ToList();
        records.Should().HaveCount(1);
        records[0].Weight.Should().Be(82.5m);
        records[0].Date.Should().Be(date);
        records[0].UserId.Should().Be(ApiTestFactory.AliceUserId);
        records[0].Deleted.Should().BeFalse();
    }

    [Fact]
    public async Task Weights_AreScopedPerUser()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);

        await alice.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));

        var bobCollection = await bob.GetFromJsonAsync<WeightsCollection>("/api/weights");

        bobCollection!.WeightRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ForAnotherUsersRecord_ReturnsNotFound()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);

        await alice.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));
        var aliceRecord = (await alice.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        var bobResponse = await bob.GetAsync($"/api/weight/{aliceRecord.WeightId}");

        bobResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_SoftDeletes_SoTheRecordDisappearsFromReads()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));
        var record = (await client.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        var delete = await client.DeleteAsync($"/api/weight/{record.WeightId}");

        using var _ = new AssertionScope();
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getById = await client.GetAsync($"/api/weight/{record.WeightId}");
        getById.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var collection = await client.GetFromJsonAsync<WeightsCollection>("/api/weights");
        collection!.WeightRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task PostThenGet_RoundTripsTheComment()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m, "after a big lunch"));

        var record = (await client.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        record.Comment.Should().Be("after a big lunch");
    }

    [Fact]
    public async Task PostWithoutComment_YieldsNoComment()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));

        var record = (await client.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        record.Comment.Should().BeNull();
    }

    [Fact]
    public async Task Put_UpdatesTheComment()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));
        var record = (await client.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        var put = await client.PutAsJsonAsync($"/api/weight/{record.WeightId}",
            new UpdateWeightRecordRequest("added later"));

        using var _ = new AssertionScope();
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updated = (await client.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();
        updated.Comment.Should().Be("added later");
    }

    [Fact]
    public async Task Put_ForAnotherUsersRecord_ReturnsNotFoundAndDoesNotChangeIt()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);
        await alice.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m, "alice's note"));
        var aliceRecord = (await alice.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        var bobPut = await bob.PutAsJsonAsync($"/api/weight/{aliceRecord.WeightId}",
            new UpdateWeightRecordRequest("bob's meddling"));

        using var _ = new AssertionScope();
        bobPut.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var stillAlices = (await alice.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();
        stillAlices.Comment.Should().Be("alice's note");
    }

    [Fact]
    public async Task Put_ForUnknownRecord_ReturnsNotFound()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var put = await client.PutAsJsonAsync($"/api/weight/{Guid.NewGuid()}",
            new UpdateWeightRecordRequest("nope"));

        put.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithOverlongComment_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var response = await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m, new string('x', 201)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_WithOverlongComment_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));
        var record = (await client.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        var put = await client.PutAsJsonAsync($"/api/weight/{record.WeightId}",
            new UpdateWeightRecordRequest(new string('x', 201)));

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
