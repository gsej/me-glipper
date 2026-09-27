namespace Api.Controllers.Report;

public record Stage1ReportEntry(DateOnly Date, decimal Weight);

public record Stage2ReportEntry(DateOnly Date, 
    decimal? RecordedWeight, 
    decimal AverageWeight, 
    decimal Bmi, 
    decimal OneWeekChange,
    decimal TwoWeekChange,
    decimal FourWeekChange,
    decimal TwelveWeekChange
    );