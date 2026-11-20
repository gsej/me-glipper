namespace Api.Controllers.Levels;

/// <summary>
/// How often a medication is taken. Unlike <see cref="DrugParameters"/> (population-average
/// pharmacokinetics), this describes the user's own regimen, so it is kept separate: it is
/// hardcoded for now (see <see cref="DosingSchedules"/>) but will later be user-alterable.
/// </summary>
public record DosingSchedule(string MedicationName, double IntervalDays);
