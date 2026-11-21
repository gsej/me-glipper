namespace Api.Controllers.Levels;

/// <summary>
/// The next-dose-due calculation. The dosing interval itself is per medication (the
/// Medications table's DefaultDosingIntervalDays).
/// </summary>
public static class NextDoseRules
{
    /// <summary>
    /// A medication whose next dose was due more than this long ago is treated as no longer
    /// being taken, so no next dose is shown for it.
    /// </summary>
    public static readonly TimeSpan NotCurrentAfterOverdue = TimeSpan.FromDays(28);

    /// <summary>When the next dose is due: the last dose time plus the interval.</summary>
    public static DateTime NextDueAt(DateTime lastTakenAt, double intervalDays) =>
        lastTakenAt.AddDays(intervalDays);

    /// <summary>
    /// Whether a medication counts as currently being taken: its next dose is due no more than
    /// <see cref="NotCurrentAfterOverdue"/> before <paramref name="now"/>.
    /// </summary>
    public static bool IsCurrent(DateTime nextDueAt, DateTime now) =>
        now - nextDueAt <= NotCurrentAfterOverdue;
}
