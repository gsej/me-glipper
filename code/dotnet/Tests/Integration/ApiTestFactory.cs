using System.Net.Http.Headers;
using Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Tests.Integration;

/// <summary>
/// Spins up the API in-process (no network, no containers) backed by a throwaway
/// SQLite file, with a known set of API keys. Reusable as-is for a cloned app:
/// change <see cref="ApiKeys"/> / the seeded config and keep the rest.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string AliceKey = "alice-key";
    public const string BobKey = "bob-key";
    public const string AliceUserId = "alice";
    public const string BobUserId = "bob";

    private const string ApiKeys =
        """[{"UserId":"alice","Key":"alice-key"},{"UserId":"bob","Key":"bob-key"}]""";

    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"me-glipper-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Added after the app's own sources, so these win over appsettings.json.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SqliteConnectionString"] = $"Data Source={_dbPath}",
                ["ApiKeys"] = ApiKeys,
            });
        });
    }

    /// <summary>Creates a client that authenticates as the given API key (null = no key).</summary>
    public HttpClient CreateClientWithKey(string? apiKey)
    {
        var client = CreateClient();
        if (apiKey != null)
        {
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
