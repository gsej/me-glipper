namespace Api.Controllers.Models;

public record VialRecord(
    Guid VialId,
    string UserId,
    string MedicationName,
    decimal InitialAmountMg,
    decimal InitialVolumeMl,
    DateOnly StartedOn);
