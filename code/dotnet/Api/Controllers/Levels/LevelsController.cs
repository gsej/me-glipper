using Api.Controllers.Models;
using Api.DataAccess;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Levels;

[ApiController]
[Route("api/levels")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class LevelsController : ControllerBase
{
    private readonly DoseRepository _doseRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LevelsController(DoseRepository doseRepository, IHttpContextAccessor httpContextAccessor)
    {
        _doseRepository = doseRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public async Task<LevelsResponse> Get()
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entities = await _doseRepository.GetAllAsync(userId);
        var now = DateTime.UtcNow;

        // Step 1: semaglutide only, instant-decay model. The other drugs (and the Bateman
        // absorption model) follow in later steps; this will then loop over the presets.
        var drug = DrugPresets.Semaglutide;
        var doses = entities
            .Where(e => string.Equals(e.MedicationName, drug.Name, StringComparison.OrdinalIgnoreCase))
            .Select(e => new DoseInput(e.TakenAt, (double)e.DoseAmountMg));

        var amount = DrugLevelCalculator.AmountRemainingMg(doses, now, drug);

        return new LevelsResponse(new[] { new DrugLevel(drug.Name, amount) });
    }
}
