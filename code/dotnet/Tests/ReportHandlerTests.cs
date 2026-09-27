using Api;
using Api.Controllers.Models;
using Api.Controllers.Report;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Tests;

public class ReportHandlerTests
{
    private readonly ReportHandler _reportHandler;
    private readonly Settings _settings;

    public ReportHandlerTests()
    {
        _settings = new Settings();
        _reportHandler = new ReportHandler(_settings);
    }
    [Fact]
    public void HandleShouldAverageWeightsForEachDay()
    {
        // i.e. if the same date has multiple weights, these will be merged into a single entry with the average weight for that date.
        
        var date1 = new DateTime(2023, 10, 1);
        var date2 = new DateTime(2023, 10, 2);
        
        var weights = new List<WeightEntity>
        {
            new (Guid.NewGuid(), "user", date1, 70, false),
            new (Guid.NewGuid(), "user",date1, 80, false),
            new (Guid.NewGuid(), "user",date1, 90, false),
            new (Guid.NewGuid(), "user",date2, 100, false),
            new (Guid.NewGuid(), "user",date2, 102, false),
            new (Guid.NewGuid(), "user",date2, 101, false)
        };

        var report = _reportHandler.GetReport(weights, 170);
        
        var date1Entry = report.Entries.SingleOrDefault(entry => entry.Date == DateOnly.FromDateTime(date1));
        var date2Entry = report.Entries.SingleOrDefault(entry => entry.Date == DateOnly.FromDateTime(date2));

        date1Entry.Should().NotBeNull();
        date1Entry!.RecordedWeight.Should().Be(80);
        date1Entry!.AverageWeight.Should().Be(80);
        
        date2Entry.Should().NotBeNull();
        date2Entry!.RecordedWeight.Should().Be(101);
        date2Entry!.AverageWeight.Should().Be(90.5m);
    }
    
    [Fact]
    public void HandleShouldReturnOneEntryForEachDateInRange()
    {
        var firstDate = new DateTime(2023, 10, 1);
        var lastDate = new DateTime(2023, 10, 5);

        var weights = new List<WeightEntity>
        {
            new(Guid.NewGuid(), "user", firstDate, 70, false), 
            new(Guid.NewGuid(), "user", lastDate, 100, false)
        };
        
        var report = _reportHandler.GetReport(weights, 170);

        report.Entries.Count.Should().Be(5);
        report.Entries.First().Date.Should().Be(DateOnly.FromDateTime(firstDate));
        report.Entries.Last().Date.Should().Be(DateOnly.FromDateTime(lastDate));
    }
    
    [Fact]
    public void HandleShouldReturnEntriesAveragingThePrevious7Days()
    {
        var firstDate = new DateTime(2023, 10, 1);

        var weights = new List<WeightEntity>
        {
            new(Guid.NewGuid(), "user", firstDate, 100, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(1), 99, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(2), 98, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(3), 97, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(4), 96, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(5), 95, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(6), 94, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(7), 93, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(8), 92, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(9), 91, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(13), 500, false),
            new(Guid.NewGuid(), "user", firstDate.AddDays(17), 500, false),
        };

        var report = _reportHandler.GetReport(weights, 170);
        
        using var _ = new AwesomeAssertions.Execution.AssertionScope();
        
        report.Entries.Count.Should().Be(18);
        
        report.Entries[0].RecordedWeight.Should().Be(100);
        report.Entries[0].AverageWeight.Should().Be(100);
        
        report.Entries[1].RecordedWeight.Should().Be(99);
        report.Entries[1].AverageWeight.Should().Be(99.5m);
        
        report.Entries[2].RecordedWeight.Should().Be(98);
        report.Entries[2].AverageWeight.Should().Be(99);
        
        report.Entries[3].RecordedWeight.Should().Be(97);
        report.Entries[3].AverageWeight.Should().Be(98.5m);
        
        report.Entries[4].RecordedWeight.Should().Be(96);
        report.Entries[4].AverageWeight.Should().Be(98);
        
        report.Entries[5].RecordedWeight.Should().Be(95);
        report.Entries[5].AverageWeight.Should().Be(97.5m);
        
        report.Entries[6].RecordedWeight.Should().Be(94);
        report.Entries[6].AverageWeight.Should().Be(97);
        
        report.Entries[7].RecordedWeight.Should().Be(93);
        report.Entries[7].AverageWeight.Should().Be(96);
        
        report.Entries[8].RecordedWeight.Should().Be(92);
        report.Entries[8].AverageWeight.Should().Be(95);
        
        report.Entries[9].RecordedWeight.Should().Be(91);
        report.Entries[9].AverageWeight.Should().Be(94);
        
        report.Entries[13].RecordedWeight.Should().Be(500);
        report.Entries[13].AverageWeight.Should().Be(194);
        
        report.Entries[17].RecordedWeight.Should().Be(500);
        report.Entries[17].AverageWeight.Should().Be(500);
    }
    
    [Fact]
    public void HandleShouldNotThrowWhenDataHasGapWiderThanTheMovingAverageWindows()
    {
        // A gap longer than the 2-week window means some days have no records within any
        // trailing window. The report should still be produced for every day in the range
        // rather than throwing on an empty average.
        var firstDate = new DateTime(2023, 10, 1);
        var lastDate = firstDate.AddDays(30);

        var weights = new List<WeightEntity>
        {
            new(Guid.NewGuid(), "user", firstDate, 100, false),
            new(Guid.NewGuid(), "user", lastDate, 90, false)
        };

        var report = _reportHandler.GetReport(weights, 170);

        using var _ = new AssertionScope();

        report.Entries.Count.Should().Be(31);

        // Deep inside the gap there are no records in any window, so the average carries
        // forward the last known value.
        var midGapEntry = report.Entries.Single(e => e.Date == DateOnly.FromDateTime(firstDate.AddDays(20)));
        midGapEntry.RecordedWeight.Should().BeNull();
        midGapEntry.AverageWeight.Should().Be(100);
    }

    [Fact]
    public void HandleShouldReturnEmptyReportWhenThereAreNoWeights()
    {
        // A user with no recorded weights (e.g. one who just opened the report) has no date
        // range to walk, so the report is empty rather than throwing.
        var report = _reportHandler.GetReport(new List<WeightEntity>(), 170);

        report.Entries.Should().BeEmpty();
    }

    [Fact]
    public void HandleShouldReturnCalculateBmi()
    {
        var firstDate = new DateTime(2023, 10, 1);
        var lastDate = new DateTime(2023, 10, 5);

        var weights = new List<WeightEntity>
        {
            new(Guid.NewGuid(), "user", firstDate, 70, false), 
            new(Guid.NewGuid(), "user", lastDate, 100, false)
        };
        
        var report = _reportHandler.GetReport(weights, 170);

        using var _ = new AssertionScope();
        
        foreach (var entry in report.Entries)
        {
            var expectedBmi = Math.Round(entry.AverageWeight / (1.7m * 1.7m), 2);
            entry.Bmi.Should().Be(expectedBmi);
        }
    }
}