namespace Api.Controllers.Levels;

/// <summary>
/// A time series of estimated drug amounts for charting. <see cref="Times"/> is the shared
/// x-axis (ascending UTC); each <see cref="DrugLevelSeries.AmountsMg"/> aligns to it
/// element-for-element, so the frontend can feed it straight to uPlot. <see cref="Now"/> is one
/// of the times: points up to it are history, later points are a projection assuming no
/// further doses.
/// </summary>
public record LevelSeriesResponse(DateTime Now, IReadOnlyList<DateTime> Times, IReadOnlyList<DrugLevelSeries> Series);

/// <summary>
/// One drug's series. <see cref="AmountsMg"/> uses the first-order absorption (Bateman) model.
/// </summary>
public record DrugLevelSeries(string MedicationName, IReadOnlyList<double> AmountsMg);
