namespace Api.Controllers.Levels;

/// <summary>
/// A time series of estimated drug amounts for charting. <see cref="Times"/> is the shared
/// x-axis (ascending UTC); each <see cref="DrugLevelSeries.AmountsMg"/> aligns to it
/// element-for-element, so the frontend can feed it straight to uPlot.
/// </summary>
public record LevelSeriesResponse(IReadOnlyList<DateTime> Times, IReadOnlyList<DrugLevelSeries> Series);

public record DrugLevelSeries(string MedicationName, IReadOnlyList<double> AmountsMg);
