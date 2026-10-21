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

        // Doses present but users omitted: a restore must carry both sections.
        var response = await client.PostAsJsonAsync("/api/backup/restore",
            new Backup(null, new[]
            {
                new DoseRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), "Semaglutide", 1.0m, false),
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Restore_ReplacesAllUsersAndDoses()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var original = new Backup(
            new[] { new User(ApiTestFactory.AliceUserId, 180) },
            new[]
            {
                new DoseRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), "Semaglutide", 1.0m, false),
                new DoseRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 2), "Semaglutide", 1.0m, false),
            });
        await client.PostAsJsonAsync("/api/backup/restore", original);

        var replacement = new Backup(
            new[] { new User(ApiTestFactory.AliceUserId, 175) },
            new[]
            {
                new DoseRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 2, 1), "Tirzepatide", 2.5m, false),
            });
        await client.PostAsJsonAsync("/api/backup/restore", replacement);

        var backup = await client.GetFromJsonAsync<Backup>("/api/backup");

        var users = backup!.Users!.ToList();
        users.Should().ContainSingle();
        users[0].heightInCm.Should().Be(175);

        var records = backup.DoseRecords!.ToList();
        records.Should().ContainSingle();
        records[0].MedicationName.Should().Be("Tirzepatide");
        records[0].DoseAmountMg.Should().Be(2.5m);
    }

    [Fact]
    public async Task Restore_RoundTripsNotes()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        await client.PostAsJsonAsync("/api/backup/restore", new Backup(
            new[] { new User(ApiTestFactory.AliceUserId, 180) },
            new[]
            {
                new DoseRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), "Semaglutide", 1.0m, false, "first dose"),
                new DoseRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 2), "Semaglutide", 1.0m, false),
            }));

        var backup = await client.GetFromJsonAsync<Backup>("/api/backup");

        var records = backup!.DoseRecords!.OrderBy(r => r.TakenAt).ToList();
        records.Should().HaveCount(2);
        records[0].Notes.Should().Be("first dose");
        records[1].Notes.Should().BeNull();
    }

    [Fact]
    public async Task Backup_IncludesSoftDeletedDoses()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        await client.PostAsJsonAsync("/api/backup/restore", new Backup(
            new[] { new User(ApiTestFactory.AliceUserId, 180) },
            new[]
            {
                new DoseRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), "Semaglutide", 1.0m, false),
                new DoseRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 2), "Semaglutide", 1.0m, true),
            }));

        var backup = await client.GetFromJsonAsync<Backup>("/api/backup");

        backup!.DoseRecords!.Should().HaveCount(2);
        backup.DoseRecords!.Should().Contain(r => r.Deleted);
    }
}
