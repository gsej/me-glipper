using Api.Controllers.Models;
using Api.DataAccess;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>The user's current vial of each medication: what it was made up with (mg in ml).</summary>
[ApiController]
[Route("api/vials")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class VialsController : ControllerBase
{
    private readonly VialRepository _vialRepository;
    private readonly MedicationRepository _medicationRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VialsController(
        VialRepository vialRepository,
        MedicationRepository medicationRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _vialRepository = vialRepository;
        _medicationRepository = medicationRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    private string UserId => (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;

    /// <summary>The user's vials, in medication display order. Medications with no vial are left out.</summary>
    [HttpGet]
    public async Task<IEnumerable<VialRecord>> GetAll() =>
        (await _vialRepository.GetAllAsync(UserId)).Select(ToRecord);

    /// <summary>Sets the user's vial of a medication, replacing any they already have.</summary>
    [HttpPut("{medicationName}")]
    public async Task<IActionResult> Put(string medicationName, [FromBody] UpdateVialRequest request)
    {
        var medication = await _medicationRepository.GetByNameAsync(medicationName);
        if (medication == null)
        {
            return BadRequest($"Unknown medication '{medicationName}'.");
        }

        await _vialRepository.UpsertAsync(new VialEntity(
            Guid.NewGuid(), UserId, medication.MedicationId, request.InitialAmountMg, request.InitialVolumeMl,
            request.StartedOn!.Value));
        return NoContent();
    }

    internal static VialRecord ToRecord(VialEntity entity) => new(
        entity.VialId, entity.UserId, entity.MedicationName, entity.InitialAmountMg, entity.InitialVolumeMl,
        entity.StartedOn);
}
