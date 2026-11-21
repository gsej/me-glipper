using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using AwesomeAssertions;

namespace Tests.Integration;

public class MedicationsApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetMedications_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/medications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMedications_ReturnsTheMedicationsInDisplayOrder()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var medications = await client.GetFromJsonAsync<List<Medication>>("/api/medications");

        medications!.Select(m => m.Name).Should().Equal("Semaglutide", "Tirzepatide", "Retatrutide");
        medications![0].Should().Be(new Medication(TestMedications.SemaglutideId, "Semaglutide", 7, 165, 0.89, 48, "#3b82f6"));
    }
}
