using Api.Controllers.Models;
using Api.DataAccess;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Tests.DataAccess;

public class DoseRepositoryTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"dose-repo-tests-{Guid.NewGuid():N}.db");
    private readonly DoseRepository _repository;

    public DoseRepositoryTests()
    {
        _repository = new DoseRepository($"Data Source={_dbPath}");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsAllFields()
    {
        var id = Guid.NewGuid();
        var takenAt = new DateTime(2025, 1, 15, 6, 30, 0, DateTimeKind.Utc);
        await _repository.AddAsync(new DoseEntity(id, "alice", takenAt, "Semaglutide", 1.0m, false));

        var loaded = await _repository.GetByIdAsync(id, "alice");

        using var _ = new AssertionScope();
        loaded.Should().NotBeNull();
        loaded!.DoseId.Should().Be(id);
        loaded.UserId.Should().Be("alice");
        loaded.MedicationName.Should().Be("Semaglutide");
        loaded.DoseAmountMg.Should().Be(1.0m);
        loaded.Deleted.Should().BeFalse();
        // Guards the "dates as UTC regardless of server timezone" behaviour.
        loaded.TakenAt.Should().Be(takenAt);
        loaded.TakenAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetAllAsync_NoArgs_ReturnsAllUsersRecordsIncludingDeleted()
    {
        // The parameterless overload backs the backup endpoint, which must read everything:
        // all users, including soft-deleted rows.
        await _repository.AddAsync(new DoseEntity(Guid.NewGuid(), "alice", new DateTime(2025, 1, 1), "Semaglutide", 1.0m, false));
        await _repository.AddAsync(new DoseEntity(Guid.NewGuid(), "alice", new DateTime(2025, 1, 2), "Semaglutide", 1.0m, true));
        await _repository.AddAsync(new DoseEntity(Guid.NewGuid(), "bob", new DateTime(2025, 1, 3), "Tirzepatide", 2.5m, false));

        var all = (await _repository.GetAllAsync()).ToList();

        all.Should().HaveCount(3);
        all.Select(r => r.UserId).Should().Contain(new[] { "alice", "bob" });
        all.Should().Contain(r => r.Deleted);
    }

    [Fact]
    public async Task AddThenGetById_NormalisesUnspecifiedDateToUtc()
    {
        // An Unspecified-kind date is stored and returned as UTC with its clock value intact,
        // enforcing the "dates as UTC regardless of server timezone" invariant.
        var id = Guid.NewGuid();
        var unspecified = new DateTime(2025, 1, 15, 6, 30, 0, DateTimeKind.Unspecified);
        await _repository.AddAsync(new DoseEntity(id, "alice", unspecified, "Semaglutide", 1.0m, false));

        var loaded = await _repository.GetByIdAsync(id, "alice");

        loaded!.TakenAt.Kind.Should().Be(DateTimeKind.Utc);
        loaded.TakenAt.Should().Be(new DateTime(2025, 1, 15, 6, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task AddThenGetById_ConvertsLocalDateToUtc()
    {
        // A Local-kind date is converted (not just relabelled) to the equivalent UTC instant.
        var id = Guid.NewGuid();
        var local = new DateTime(2025, 1, 15, 6, 30, 0, DateTimeKind.Local);
        await _repository.AddAsync(new DoseEntity(id, "alice", local, "Semaglutide", 1.0m, false));

        var loaded = await _repository.GetByIdAsync(id, "alice");

        loaded!.TakenAt.Kind.Should().Be(DateTimeKind.Utc);
        loaded.TakenAt.Should().Be(local.ToUniversalTime());
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotReturnAnotherUsersRecord()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(new DoseEntity(id, "alice", new DateTime(2025, 1, 15), "Semaglutide", 1.0m, false));

        var asBob = await _repository.GetByIdAsync(id, "bob");

        asBob.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_FiltersByUserAndExcludesDeleted()
    {
        await _repository.AddAsync(new DoseEntity(Guid.NewGuid(), "alice", new DateTime(2025, 1, 1), "Semaglutide", 1.0m, false));
        await _repository.AddAsync(new DoseEntity(Guid.NewGuid(), "alice", new DateTime(2025, 1, 2), "Semaglutide", 1.0m, true));
        await _repository.AddAsync(new DoseEntity(Guid.NewGuid(), "bob", new DateTime(2025, 1, 3), "Tirzepatide", 2.5m, false));

        var aliceRecords = (await _repository.GetAllAsync("alice")).ToList();

        aliceRecords.Should().ContainSingle();
        aliceRecords[0].DoseAmountMg.Should().Be(1.0m);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_RowRemainsButIsExcludedFromReads()
    {
        var id = Guid.NewGuid();
        var entity = new DoseEntity(id, "alice", new DateTime(2025, 1, 15), "Semaglutide", 1.0m, false);
        await _repository.AddAsync(entity);

        await _repository.DeleteAsync(entity);

        using var _ = new AssertionScope();
        (await _repository.GetByIdAsync(id, "alice")).Should().BeNull();
        (await _repository.GetAllAsync("alice")).Should().BeEmpty();
        // Still physically present (backup reads everything, including deleted).
        (await _repository.GetAllAsync()).Should().ContainSingle(e => e.DoseId == id && e.Deleted);
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesTheRowEntirely()
    {
        var id = Guid.NewGuid();
        var entity = new DoseEntity(id, "alice", new DateTime(2025, 1, 15), "Semaglutide", 1.0m, false);
        await _repository.AddAsync(entity);

        await _repository.HardDeleteAsync(entity);

        (await _repository.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsTheNotes()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(
            new DoseEntity(id, "alice", new DateTime(2025, 1, 15), "Semaglutide", 1.0m, false, "first dose of the week"));

        var loaded = await _repository.GetByIdAsync(id, "alice");

        loaded!.Notes.Should().Be("first dose of the week");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_NormalisesEmptyOrWhitespaceNotesToNull(string? notes)
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(
            new DoseEntity(id, "alice", new DateTime(2025, 1, 15), "Semaglutide", 1.0m, false, notes));

        var loaded = await _repository.GetByIdAsync(id, "alice");

        loaded!.Notes.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_TrimsSurroundingWhitespaceFromNotes()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(
            new DoseEntity(id, "alice", new DateTime(2025, 1, 15), "Semaglutide", 1.0m, false, "  felt fine  "));

        var loaded = await _repository.GetByIdAsync(id, "alice");

        loaded!.Notes.Should().Be("felt fine");
    }

    [Fact]
    public async Task UpdateAsync_SetsAndClearsTheNotes()
    {
        var id = Guid.NewGuid();
        var entity = new DoseEntity(id, "alice", new DateTime(2025, 1, 15), "Semaglutide", 1.0m, false);
        await _repository.AddAsync(entity);

        entity.Notes = "mild nausea";
        await _repository.UpdateAsync(entity);
        (await _repository.GetByIdAsync(id, "alice"))!.Notes.Should().Be("mild nausea");

        entity.Notes = null;
        await _repository.UpdateAsync(entity);
        (await _repository.GetByIdAsync(id, "alice"))!.Notes.Should().BeNull();
    }
}
