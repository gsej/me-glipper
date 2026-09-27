using Api.Controllers.Models;

namespace Api.Controllers.Report;

public class ReportHandler
{
    private readonly Settings _settings;

    public ReportHandler(Settings settings)
    {
        _settings = settings;
    }

    public WeightReport GetReport(IEnumerable<WeightEntity> records, int heightInCm)
    {
        // stage 1, reduce the records to a single entry for each date, averaging the weights
        var stage1Entries = records
            .GroupBy(record => DateOnly.FromDateTime(record.Date))
            .Select(group => new Stage1ReportEntry(
                group.Key,
                group.Average(record => record.Weight)))
            .OrderBy(record => record.Date)
            .ToList();

        // No records means no date range to walk — return an empty report rather than
        // throwing on Min()/Max() of an empty sequence (e.g. a new user opening the
        // report before logging any weight).
        if (stage1Entries.Count == 0)
        {
            return new WeightReport();
        }

        // stage two.
        var firstDate = stage1Entries.Min(entry => entry.Date);
        var lastDate = stage1Entries.Max(entry => entry.Date);

        var stage2Entries = new List<Stage2ReportEntry>();

        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            // create an entry. it will have the current date. 
            // The weight will be the average of the weights of the previous 7 days (i.e entries from date.AddDays(-7) to the current date, taking in account missing days).
            var previousEntries = stage1Entries
                .Where(entry => entry.Date > date.AddDays(-_settings.AverageWeightWindowInDays) && entry.Date <= date)
                .OrderBy(entry => entry.Date)
                .Select(entry => entry.Weight)
                .ToList();

            decimal? recordedWeight;
            decimal movingAverageWeight;

            if (previousEntries.Count == 0)
            {
                recordedWeight = null;
                movingAverageWeight = Math.Round(stage2Entries.Single(e => e.Date == date.AddDays(-1)).AverageWeight, 2);
            }
            else
            {
                recordedWeight = previousEntries.Last();
                movingAverageWeight = previousEntries.Average();
            }

            var heightInMetres = (decimal)heightInCm / 100;

            var bmi = Math.Round(movingAverageWeight / (heightInMetres * heightInMetres), 2);

            var oneWeekChange = CalculateLastNWeekChange(stage2Entries, date, movingAverageWeight, 1);

            var twoWeekAverage = CalculateMovingAverageWeight(stage1Entries, date, 14, movingAverageWeight);
            var twoWeekChange = CalculateLastNWeekChange(stage2Entries, date, twoWeekAverage, 2);

            var fourWeekAverage = CalculateMovingAverageWeight(stage1Entries, date, 28, movingAverageWeight);
            var fourWeekChange = CalculateLastNWeekChange(stage2Entries, date, fourWeekAverage, 4);

            var twelveWeekAverage = CalculateMovingAverageWeight(stage1Entries, date, 84, movingAverageWeight);
            var twelveWeekChange = CalculateLastNWeekChange(stage2Entries, date, twelveWeekAverage, 12);

            var entry = new Stage2ReportEntry(date, 
                recordedWeight, 
                movingAverageWeight, 
                bmi, 
                oneWeekChange,
                twoWeekChange,
                fourWeekChange,
                twelveWeekChange
                );
            stage2Entries.Add(entry);
        }

        var report = new WeightReport
        {
            Entries = stage2Entries
        };

        return report;
    }

    private decimal CalculateMovingAverageWeight(IList<Stage1ReportEntry> stage1Entries, DateOnly date, int windowInDays, decimal fallbackWeight)
    {
        var previousEntries = stage1Entries
            .Where(entry => entry.Date > date.AddDays(-windowInDays) && entry.Date <= date)
            .OrderBy(entry => entry.Date)
            .Select(entry => entry.Weight)
            .ToList();

        // A gap wider than the window leaves no records to average. This window always
        // contains the 7-day window, so the day's (carried-forward) moving average is the
        // best available estimate and keeps the report from throwing on sparse data.
        return previousEntries.Count == 0
            ? fallbackWeight
            : previousEntries.Average();
    }

    private decimal CalculateLastNWeekChange(List<Stage2ReportEntry> stage2Entries, DateOnly date, decimal movingAverageWeight, int weeks)
    {
        var lastWeeksEntry = stage2Entries.SingleOrDefault(e => e.Date == date.AddDays(-7 * weeks));

        var weeksChange = lastWeeksEntry != null
            ? Math.Round(movingAverageWeight - lastWeeksEntry.AverageWeight, 2)
            : 0;
        return weeksChange;
    }
}