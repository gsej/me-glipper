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

        // Instant-decay model for each known drug. Only report a drug the user has actually
        // dosed (no zero rows for untaken medications). The Bateman absorption model will
        // later swap in behind DrugLevelCalculator without changing this loop.
        var levels = new List<DrugLevel>();
        foreach (var drug in DrugPresets.All)
        {
            var doses = entities
                .Where(e => string.Equals(e.MedicationName, drug.Name, StringComparison.OrdinalIgnoreCase))
                .Select(e => new DoseInput(e.TakenAt, (double)e.DoseAmountMg))
                .ToList();

            if (doses.Count == 0)
                continue;

            var amount = DrugLevelCalculator.AmountRemainingMg(doses, now, drug);
            levels.Add(new DrugLevel(drug.Name, amount));
        }

        return new LevelsResponse(levels);
    }
}
