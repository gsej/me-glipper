namespace Api.Controllers.Levels;

/// <summary>
/// A time series of estimated drug amounts for charting. <see cref="Times"/> is the shared
/// x-axis (ascending UTC); each <see cref="DrugLevelSeries.AmountsMg"/> aligns to it
/// element-for-element, so the frontend can feed it straight to uPlot.
/// </summary>
public record LevelSeriesResponse(IReadOnlyList<DateTime> Times, IReadOnlyList<DrugLevelSeries> Series);

/// <summary>
/// One drug's series. <see cref="AmountsMg"/> uses the first-order absorption (Bateman) model.
/// <see cref="InstantAmountsMg"/> is the instant-decay model, kept TEMPORARILY as a comparison
/// line while the absorption curve is checked; remove it afterwards.
/// </summary>
public record DrugLevelSeries(
    string MedicationName, IReadOnlyList<double> AmountsMg, IReadOnlyList<double> InstantAmountsMg);
