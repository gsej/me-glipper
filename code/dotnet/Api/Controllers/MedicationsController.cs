using Api.Controllers.Models;
using Api.DataAccess;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/medications")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class MedicationsController : ControllerBase
{
    private readonly MedicationRepository _medicationRepository;

    public MedicationsController(MedicationRepository medicationRepository)
    {
        _medicationRepository = medicationRepository;
    }

    /// <summary>The medications a dose can be recorded against, in display order.</summary>
    [HttpGet]
    public async Task<IEnumerable<Medication>> GetAll() =>
        (await _medicationRepository.GetAllAsync())
            .Select(m => new Medication(
                m.MedicationId,
                m.Name,
                m.DefaultDosingIntervalDays,
                m.HalfLifeHours,
                m.Bioavailability,
                m.TmaxHours,
                m.ChartColor));
}
