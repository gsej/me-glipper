using Api.Controllers.Models;
using Api.DataAccess;
using AwesomeAssertions;

namespace Tests.DataAccess;

public class MedicationRepositoryTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"medication-repo-tests-{Guid.NewGuid():N}.db");
    private readonly MedicationRepository _repository;

    public MedicationRepositoryTests()
    {
        DatabaseMigrator.Migrate($"Data Source={_dbPath}");
        _repository = new MedicationRepository($"Data Source={_dbPath}");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTheSeededMedicationsInDisplayOrder()
    {
        var medications = await _repository.GetAllAsync();

        medications.Should().Equal(
            new MedicationEntity(TestMedications.SemaglutideId, "Semaglutide", 7, 165, 0.89, 48, "#3b82f6", 1),
            new MedicationEntity(TestMedications.TirzepatideId, "Tirzepatide", 5, 117, 0.80, 24, "#10b981", 2),
            new MedicationEntity(TestMedications.RetatrutideId, "Retatrutide", 6, 144, 1.0, 36, "#8b5cf6", 3));
    }

    [Theory]
    [InlineData("Tirzepatide")]
    [InlineData("tirzepatide")]
    [InlineData("TIRZEPATIDE")]
    public async Task GetByNameAsync_IgnoresCase(string name)
    {
        var medication = await _repository.GetByNameAsync(name);

        medication.Should().NotBeNull();
        medication!.MedicationId.Should().Be(TestMedications.TirzepatideId);
        medication.Name.Should().Be("Tirzepatide");
    }

    [Fact]
    public async Task GetByNameAsync_ForAnUnknownName_ReturnsNull()
    {
        (await _repository.GetByNameAsync("Ozempic")).Should().BeNull();
    }
}
