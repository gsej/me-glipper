using Api.Controllers.Models;
using Api.DataAccess;
using AwesomeAssertions;

namespace Tests.DataAccess;

public class UserRepositoryTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"user-repo-tests-{Guid.NewGuid():N}.db");
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        DatabaseMigrator.Migrate($"Data Source={_dbPath}");
        _repository = new UserRepository($"Data Source={_dbPath}");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsTheUser()
    {
        await _repository.AddAsync(new UserEntity("alice"));

        var loaded = await _repository.GetByIdAsync("alice");

        loaded.Should().NotBeNull();
        loaded!.UserId.Should().Be("alice");
    }

    [Fact]
    public async Task GetByIdAsync_ForUnknownUser_ReturnsNull()
    {
        var loaded = await _repository.GetByIdAsync("nobody");

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task EnsureExistsAsync_ForUnknownUser_CreatesRow()
    {
        await _repository.EnsureExistsAsync("alice");

        var loaded = await _repository.GetByIdAsync("alice");
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task EnsureExistsAsync_ForExistingUser_IsIdempotent()
    {
        await _repository.AddAsync(new UserEntity("alice"));

        await _repository.EnsureExistsAsync("alice");

        var all = await _repository.GetAllAsync();
        all.Should().ContainSingle(u => u.UserId == "alice");
    }
}
