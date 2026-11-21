namespace Api.Controllers.Models;

/// <summary>
/// A medication the app knows about, with its default dosing interval, the
/// population-average pharmacokinetic parameters used for the level estimates, and how it
/// is shown. Reference data, seeded by migrations (see docs/medications.md).
/// </summary>
public record MedicationEntity(
    int MedicationId,
    string Name,
    double DefaultDosingIntervalDays,
    double HalfLifeHours,
    double Bioavailability,
    double TmaxHours,
    string ChartColor,
    int DisplayOrder);
