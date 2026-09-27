using Api.Filters;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;

namespace Tests;

public class ApiKeyTests
{
    [Fact]
    public void TrySerialize()
    {
        var keys = new List<ApiKey>
        {
            new ("user1", "key1"),
            new ("user2", "key2"),
        };

        var json = System.Text.Json.JsonSerializer.Serialize(keys);

        json.Should().Be("[{\"UserId\":\"user1\",\"Key\":\"key1\"},{\"UserId\":\"user2\",\"Key\":\"key2\"}]");
    }

    private static IConfiguration ConfigWithApiKeys(string? apiKeysJson) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiKeys"] = apiKeysJson })
            .Build();

    [Fact]
    public void ApiKeyStore_ParsesDistinctKeys()
    {
        var store = new ApiKeyStore(ConfigWithApiKeys(
            """[{"UserId":"alice","Key":"alice-key"},{"UserId":"bob","Key":"bob-key"}]"""));

        store.Keys.Should().HaveCount(2);
    }

    [Fact]
    public void ApiKeyStore_WithDuplicateKeyValue_Throws()
    {
        // Two users sharing the same key value is ambiguous and must be rejected at startup.
        var act = () => new ApiKeyStore(ConfigWithApiKeys(
            """[{"UserId":"alice","Key":"shared"},{"UserId":"bob","Key":"shared"}]"""));

        act.Should().Throw<InvalidOperationException>().WithMessage("*duplicate key value*");
    }

    [Fact]
    public void ApiKeyStore_WithMissingConfig_Throws()
    {
        var act = () => new ApiKeyStore(ConfigWithApiKeys(null));

        act.Should().Throw<InvalidOperationException>();
    }
}
