namespace Api.Controllers.Models;

public record Medication(
    int MedicationId,
    string Name,
    double DefaultDosingIntervalDays,
    double HalfLifeHours,
    double Bioavailability,
    double TmaxHours,
    string ChartColor);
