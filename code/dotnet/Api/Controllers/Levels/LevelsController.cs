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
    // each dose's peak is captured exactly.
    private static readonly TimeSpan SeriesStep = TimeSpan.FromHours(6);

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
        var dosed = await GetDosedDrugsAsync();
        var now = DateTime.UtcNow;

        var levels = dosed
            .Select(x => new DrugLevel(x.Drug.Name, DrugLevelCalculator.AmountRemainingMg(x.Doses, now, x.Drug)))
            .ToList();

        return new LevelsResponse(levels);
    }

    [HttpGet("series")]
    public async Task<LevelSeriesResponse> GetSeries()
    {
        var dosed = await GetDosedDrugsAsync();
        var now = DateTime.UtcNow;

        if (dosed.Count == 0)
            return new LevelSeriesResponse(Array.Empty<DateTime>(), Array.Empty<DrugLevelSeries>());

        var allDoseTimes = dosed.SelectMany(x => x.Doses).Select(d => d.TakenAt).ToList();
        var start = allDoseTimes.Min();
        var times = BuildTimeGrid(start, now, allDoseTimes);

        var series = dosed
            .Select(x => new DrugLevelSeries(
                x.Drug.Name, DrugLevelCalculator.AmountRemainingSeriesMg(x.Doses, times, x.Drug)))
            .ToList();

        return new LevelSeriesResponse(times, series);
    }

    /// <summary>
    /// The presets the current user has actually dosed, paired with those doses. Drugs with no
    /// doses are excluded (no zero rows / empty series for untaken medications).
    /// </summary>
    private async Task<IReadOnlyList<(DrugParameters Drug, List<DoseInput> Doses)>> GetDosedDrugsAsync()
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        var entities = await _doseRepository.GetAllAsync(userId);

        return DrugPresets.All
            .Select(drug => (Drug: drug, Doses: entities
                .Where(e => string.Equals(e.MedicationName, drug.Name, StringComparison.OrdinalIgnoreCase))
                .Select(e => new DoseInput(e.TakenAt, (double)e.DoseAmountMg))
                .ToList()))
            .Where(x => x.Doses.Count > 0)
            .ToList();
    }

    /// <summary>
    /// A shared ascending x-axis from the first dose to now: a fixed-step grid plus the exact
    /// dose instants (so peaks land on a sample) and <paramref name="now"/> as the final point.
    /// </summary>
    private static IReadOnlyList<DateTime> BuildTimeGrid(DateTime start, DateTime now, IEnumerable<DateTime> doseTimes)
    {
        var grid = new SortedSet<DateTime>();
        for (var t = start; t < now; t += SeriesStep)
            grid.Add(t);
        grid.Add(now);
        foreach (var dt in doseTimes)
            if (dt >= start && dt <= now)
                grid.Add(dt);
        return grid.ToList();
    }
}
