using Api.Controllers.Models;
using Api.DataAccess;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Tests.DataAccess;

public class VialRepositoryTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"vial-repo-tests-{Guid.NewGuid():N}.db");
    private readonly VialRepository _repository;
    private static readonly DateOnly Started = new(2025, 3, 14);

    public VialRepositoryTests()
    {
        DatabaseMigrator.Migrate($"Data Source={_dbPath}");
        _repository = new VialRepository($"Data Source={_dbPath}");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task UpsertThenGetAll_RoundTripsAllFields()
    {
        var id = Guid.NewGuid();
        await _repository.UpsertAsync(new VialEntity(id, "alice", TestMedications.TirzepatideId, 10.5m, 2.25m, Started));

        var vials = await _repository.GetAllAsync("alice");

        using var _ = new AssertionScope();
        vials.Should().ContainSingle();
        vials[0].VialId.Should().Be(id);
        vials[0].UserId.Should().Be("alice");
        vials[0].MedicationId.Should().Be(TestMedications.TirzepatideId);
        vials[0].MedicationName.Should().Be("Tirzepatide");
        vials[0].InitialAmountMg.Should().Be(10.5m);
        vials[0].InitialVolumeMl.Should().Be(2.25m);
        vials[0].StartedOn.Should().Be(Started);
    }

    [Fact]
    public async Task Upsert_ForAMedicationTheUserAlreadyHasAVialOf_ReplacesTheAmountsAndDateAndKeepsTheId()
    {
        var id = Guid.NewGuid();
        await _repository.UpsertAsync(new VialEntity(id, "alice", TestMedications.SemaglutideId, 5m, 2m, Started));

        await _repository.UpsertAsync(new VialEntity(Guid.NewGuid(), "alice", TestMedications.SemaglutideId, 10m, 3m, new DateOnly(2025, 4, 1)));

        var vials = await _repository.GetAllAsync("alice");
        vials.Should().ContainSingle();
        vials[0].VialId.Should().Be(id);
        vials[0].InitialAmountMg.Should().Be(10m);
        vials[0].InitialVolumeMl.Should().Be(3m);
        vials[0].StartedOn.Should().Be(new DateOnly(2025, 4, 1));
    }

    [Fact]
    public async Task GetAll_ForAUser_ReturnsOnlyTheirVials_InMedicationDisplayOrder()
    {
        await _repository.UpsertAsync(new VialEntity(Guid.NewGuid(), "alice", TestMedications.RetatrutideId, 12m, 2m, Started));
        await _repository.UpsertAsync(new VialEntity(Guid.NewGuid(), "alice", TestMedications.SemaglutideId, 5m, 2m, Started));
        await _repository.UpsertAsync(new VialEntity(Guid.NewGuid(), "bob", TestMedications.SemaglutideId, 3m, 1m, Started));

        var vials = await _repository.GetAllAsync("alice");

        vials.Select(v => v.MedicationName).Should().Equal("Semaglutide", "Retatrutide");
        vials.Should().OnlyContain(v => v.UserId == "alice");
    }

    [Fact]
    public async Task GetAll_NoArgs_ReturnsEveryUsersVials()
    {
        await _repository.UpsertAsync(new VialEntity(Guid.NewGuid(), "alice", TestMedications.SemaglutideId, 5m, 2m, Started));
        await _repository.UpsertAsync(new VialEntity(Guid.NewGuid(), "bob", TestMedications.SemaglutideId, 3m, 1m, Started));

        var vials = await _repository.GetAllAsync();

        vials.Select(v => v.UserId).Should().BeEquivalentTo("alice", "bob");
    }

    [Fact]
    public async Task HardDelete_RemovesTheVial()
    {
        var vial = new VialEntity(Guid.NewGuid(), "alice", TestMedications.SemaglutideId, 5m, 2m, Started);
        await _repository.UpsertAsync(vial);

        await _repository.HardDeleteAsync(vial);

        (await _repository.GetAllAsync("alice")).Should().BeEmpty();
    }
}
