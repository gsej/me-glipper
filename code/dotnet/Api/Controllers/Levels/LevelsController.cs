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
    // Sampling resolution for the chart series. 6 h is plenty for these multi-day half-lives
    // and keeps payloads modest even over a long history; dose instants are added on top so
    // each dose's start is captured exactly (the first-order peak comes later, at ~Tmax).
    private static readonly TimeSpan SeriesStep = TimeSpan.FromHours(6);

    // How far past now the series runs, as a fraction of the history span (first dose -> now).
    // The charts show past and future 80/20, i.e. future = past / 4, and no range can show more
    // past than the whole history, so this covers every range in one response. The projection
    // assumes no further doses.
    private const double ForecastFraction = 0.25;

    private readonly DoseRepository _doseRepository;
    private readonly MedicationRepository _medicationRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LevelsController(
        DoseRepository doseRepository,
        MedicationRepository medicationRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _doseRepository = doseRepository;
        _medicationRepository = medicationRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public async Task<LevelsResponse> Get()
    {
        var dosed = await GetDosedDrugsAsync();
        var now = DateTime.UtcNow;

        var levels = dosed
            .Select(x => new DrugLevel(x.Drug.Name, DrugLevelCalculator.AmountRemainingMg(x.Doses, now, x.Drug, AbsorptionModel.FirstOrder)))
            .ToList();

        return new LevelsResponse(levels);
    }

    [HttpGet("series")]
    public async Task<LevelSeriesResponse> GetSeries()
    {
        var dosed = await GetDosedDrugsAsync();
        var now = DateTime.UtcNow;

        if (dosed.Count == 0)
            return new LevelSeriesResponse(now, Array.Empty<DateTime>(), Array.Empty<DrugLevelSeries>());

        var allDoseTimes = dosed.SelectMany(x => x.Doses).Select(d => d.TakenAt).ToList();
        var start = allDoseTimes.Min();
        var end = now + (now - start) * ForecastFraction;
        var times = BuildTimeGrid(start, now, end, allDoseTimes);

        var series = dosed
            .Select(x => new DrugLevelSeries(
                x.Drug.Name,
                DrugLevelCalculator.AmountRemainingSeriesMg(x.Doses, times, x.Drug, AbsorptionModel.FirstOrder)))
            .ToList();

        return new LevelSeriesResponse(now, times, series);
    }

    /// <summary>
    /// When the next dose of each medication is due (last dose + its dosing interval). Only
    /// medications currently being taken are included: never-taken ones, and ones whose next
    /// dose was due more than <see cref="NextDoseRules.NotCurrentAfterOverdue"/> ago, are left out.
    /// </summary>
    [HttpGet("next-doses")]
    public async Task<NextDosesResponse> GetNextDoses()
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entities = await _doseRepository.GetAllAsync(userId);
        var medications = await _medicationRepository.GetAllAsync();
        var now = DateTime.UtcNow;

        var nextDoses = new List<NextDose>();
        foreach (var medication in medications)
        {
            var last = entities
                .Where(e => e.MedicationId == medication.MedicationId)
                .MaxBy(e => e.TakenAt);
            if (last is null)
                continue;

            var nextDueAt = NextDoseRules.NextDueAt(last.TakenAt, medication.DefaultDosingIntervalDays);
            if (!NextDoseRules.IsCurrent(nextDueAt, now))
                continue;

            nextDoses.Add(new NextDose(
                medication.Name,
                medication.DefaultDosingIntervalDays,
                last.TakenAt,
                (double)last.DoseAmountMg,
                nextDueAt));
        }

        return new NextDosesResponse(nextDoses);
    }

    /// <summary>
    /// The medications the current user has actually dosed (in display order), as calculator
    /// parameters paired with those doses. Medications with no doses are excluded (no zero rows /
    /// empty series for untaken medications).
    /// </summary>
    private async Task<IReadOnlyList<(DrugParameters Drug, List<DoseInput> Doses)>> GetDosedDrugsAsync()
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entities = await _doseRepository.GetAllAsync(userId);
        var medications = await _medicationRepository.GetAllAsync();

        return medications
            .Select(m => (
                Drug: new DrugParameters(m.Name, m.HalfLifeHours, m.Bioavailability, m.TmaxHours),
                Doses: entities
                    .Where(e => e.MedicationId == m.MedicationId)
                    .Select(e => new DoseInput(e.TakenAt, (double)e.DoseAmountMg))
                    .ToList()))
            .Where(x => x.Doses.Count > 0)
            .ToList();
    }

    /// <summary>
    /// A shared ascending x-axis from the first dose to <paramref name="end"/> (past now, for the
    /// projection): a fixed-step grid plus the exact dose instants (so each dose's start lands on a
    /// sample), <paramref name="now"/> (where history meets projection) and <paramref name="end"/>.
    /// </summary>
    private static IReadOnlyList<DateTime> BuildTimeGrid(DateTime start, DateTime now, DateTime end, IEnumerable<DateTime> doseTimes)
    {
        var grid = new SortedSet<DateTime>();
        for (var t = start; t < end; t += SeriesStep)
            grid.Add(t);
        grid.Add(now);
        grid.Add(end);
        foreach (var dt in doseTimes)
            if (dt >= start && dt <= now)
                grid.Add(dt);
        return grid.ToList();
    }
}
