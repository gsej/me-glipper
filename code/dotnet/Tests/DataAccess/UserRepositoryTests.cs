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
        _repository = new UserRepository($"Data Source={_dbPath}");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsTheUser()
    {
        await _repository.AddAsync(new UserEntity("alice", 172));

        var loaded = await _repository.GetByIdAsync("alice");

        loaded.Should().NotBeNull();
        loaded!.UserId.Should().Be("alice");
        loaded.HeightInCm.Should().Be(172);
    }

    [Fact]
    public async Task GetByIdAsync_ForUnknownUser_ReturnsNull()
    {
        var loaded = await _repository.GetByIdAsync("nobody");

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task EnsureExistsAsync_ForUnknownUser_CreatesRowWithGivenHeight()
    {
        await _repository.EnsureExistsAsync("alice", 1000);

        var loaded = await _repository.GetByIdAsync("alice");
        loaded.Should().NotBeNull();
        loaded!.HeightInCm.Should().Be(1000);
    }

    [Fact]
    public async Task EnsureExistsAsync_ForExistingUser_LeavesHeightUnchanged()
    {
        await _repository.AddAsync(new UserEntity("alice", 172));

        await _repository.EnsureExistsAsync("alice", 1000);

        var loaded = await _repository.GetByIdAsync("alice");
        loaded!.HeightInCm.Should().Be(172);
    }
}
