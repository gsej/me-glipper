namespace Api.Controllers.Report;

public class WeightReport
{
    public IList<Stage2ReportEntry> Entries { get; init; } = new List<Stage2ReportEntry>();
}