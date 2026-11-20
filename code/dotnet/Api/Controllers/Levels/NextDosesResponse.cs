namespace Api.Controllers.Levels;

/// <summary>When the next dose of each currently-taken medication is due.</summary>
public record NextDosesResponse(IEnumerable<NextDose> NextDoses);

public record NextDose(
    string MedicationName,
    double IntervalDays,
    DateTime LastTakenAt,
    double LastDoseAmountMg,
    DateTime NextDueAt);
