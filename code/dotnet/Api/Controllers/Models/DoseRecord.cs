namespace Api.Controllers.Models;

public record DoseRecord(
    Guid DoseId,
    string UserId,
    DateTime TakenAt,
    string MedicationName,
    decimal DoseAmountMg,
    bool Deleted,
    string? Notes = null);
