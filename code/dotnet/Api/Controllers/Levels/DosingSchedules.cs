namespace Api.Controllers.Levels;

/// <summary>
/// Hardcoded dosing schedules, one per tracked drug, and the next-dose-due calculation.
/// The intervals will later be user-alterable.
/// </summary>
public static class DosingSchedules
{
    /// <summary>
    /// A medication whose next dose was due more than this long ago is treated as no longer
    /// being taken, so no next dose is shown for it.
    /// </summary>
    public static readonly TimeSpan NotCurrentAfterOverdue = TimeSpan.FromDays(28);

    public static readonly DosingSchedule Semaglutide = new("Semaglutide", IntervalDays: 7);
    public static readonly DosingSchedule Tirzepatide = new("Tirzepatide", IntervalDays: 5);
    public static readonly DosingSchedule Retatrutide = new("Retatrutide", IntervalDays: 6);

    /// <summary>All schedules, in display order. Declared last so the fields above are
    /// initialised first.</summary>
    public static readonly IReadOnlyList<DosingSchedule> All = new[]
    {
        Semaglutide,
        Tirzepatide,
        Retatrutide,
    };

    /// <summary>When the next dose is due: the last dose time plus the interval.</summary>
    public static DateTime NextDueAt(DateTime lastTakenAt, DosingSchedule schedule) =>
        lastTakenAt.AddDays(schedule.IntervalDays);

    /// <summary>
    /// Whether a medication counts as currently being taken: its next dose is due no more than
    /// <see cref="NotCurrentAfterOverdue"/> before <paramref name="now"/>.
    /// </summary>
    public static bool IsCurrent(DateTime nextDueAt, DateTime now) =>
        now - nextDueAt <= NotCurrentAfterOverdue;
}
