using System.Text.Json;
using Api.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public record ApiKey(string UserId, string Key);

/// <summary>
/// Parses and validates the API keys from configuration once, at startup. Registered as a
/// singleton and resolved during app startup so a bad <c>ApiKeys</c> config (missing, invalid
/// JSON, or duplicate key values) fails the deploy immediately rather than surfacing as a
/// runtime error on a request.
/// </summary>
public class ApiKeyStore
{
    public IReadOnlyList<ApiKey> Keys { get; }

    public ApiKeyStore(IConfiguration configuration)
    {
        var apiKeysJson = configuration["ApiKeys"]
                          ?? throw new InvalidOperationException("ApiKeys configuration is missing.");

        var keys = JsonSerializer.Deserialize<List<ApiKey>>(apiKeysJson)
                   ?? throw new InvalidOperationException("ApiKeys configuration is invalid.");

        // A key value shared by more than one entry is ambiguous — a request presenting it
        // could resolve to different users. Reject it here rather than letting SingleOrDefault
        // throw (a 500) on the unlucky request that happens to present the duplicated value.
        var duplicate = keys.GroupBy(k => k.Key).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            throw new InvalidOperationException(
                $"ApiKeys configuration contains a duplicate key value ('{duplicate.Key}'); each API key must be unique.");
        }

        Keys = keys;
    }
}

public class ApiKeyAuthFilter : IAsyncAuthorizationFilter
{
    public const string UserIdKeyname = "UserId";

    // Height seeded for a user's profile row the first time they authenticate. It is
    // deliberately absurd (1000cm) so an unset height is obvious in reports rather than
    // masquerading as a plausible value. Real heights arrive via the backup/restore endpoint.
    public const int SeededHeightInCm = 1000;

    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly ApiKeyStore _apiKeyStore;
    private readonly UserRepository _userRepository;

    public ApiKeyAuthFilter(ApiKeyStore apiKeyStore, UserRepository userRepository)
    {
        _apiKeyStore = apiKeyStore;
        _userRepository = userRepository;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var matchingApiKey = _apiKeyStore.Keys.FirstOrDefault(apiKey => apiKey.Key == providedApiKey);
        if (matchingApiKey == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Guarantee every authenticated identity has a profile row so downstream code can
        // rely on it. New users get a sentinel height until a real one is restored.
        await _userRepository.EnsureExistsAsync(matchingApiKey.UserId, SeededHeightInCm);

        context.HttpContext.Items[UserIdKeyname] = matchingApiKey.UserId;
    }
}
