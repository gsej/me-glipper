using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using AwesomeAssertions;

namespace Tests.Integration;

public class BackupApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Restore_WithNullBody_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var response = await client.PostAsJsonAsync<Backup?>("/api/backup/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Restore_WithMissingSection_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        // Weights present but users omitted: a restore must carry both sections.
        var response = await client.PostAsJsonAsync("/api/backup/restore",
            new Backup(null, new[]
            {
                new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), 80m, false),
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Restore_ReplacesAllUsersAndWeights()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var original = new Backup(
            new[] { new User(ApiTestFactory.AliceUserId, 180) },
            new[]
            {
                new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), 80m, false),
                new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 2), 81m, false),
            });
        await client.PostAsJsonAsync("/api/backup/restore", original);

        var replacement = new Backup(
            new[] { new User(ApiTestFactory.AliceUserId, 175) },
            new[]
            {
                new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 2, 1), 79m, false),
            });
        await client.PostAsJsonAsync("/api/backup/restore", replacement);

        var backup = await client.GetFromJsonAsync<Backup>("/api/backup");

        var users = backup!.Users!.ToList();
        users.Should().ContainSingle();
        users[0].heightInCm.Should().Be(175);

        var records = backup.WeightRecords!.ToList();
        records.Should().ContainSingle();
        records[0].Weight.Should().Be(79m);
    }

    [Fact]
    public async Task Restore_RoundTripsComments()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        await client.PostAsJsonAsync("/api/backup/restore", new Backup(
            new[] { new User(ApiTestFactory.AliceUserId, 180) },
            new[]
            {
                new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), 80m, false, "new year"),
                new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 2), 81m, false),
            }));

        var backup = await client.GetFromJsonAsync<Backup>("/api/backup");

        var records = backup!.WeightRecords!.OrderBy(r => r.Date).ToList();
        records.Should().HaveCount(2);
        records[0].Comment.Should().Be("new year");
        records[1].Comment.Should().BeNull();
    }

    [Fact]
    public async Task Backup_IncludesSoftDeletedWeights()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        await client.PostAsJsonAsync("/api/backup/restore", new Backup(
            new[] { new User(ApiTestFactory.AliceUserId, 180) },
            new[]
            {
                new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), 80m, false),
                new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 2), 81m, true),
            }));

        var backup = await client.GetFromJsonAsync<Backup>("/api/backup");

        backup!.WeightRecords!.Should().HaveCount(2);
        backup.WeightRecords!.Should().Contain(r => r.Deleted);
    }
}
