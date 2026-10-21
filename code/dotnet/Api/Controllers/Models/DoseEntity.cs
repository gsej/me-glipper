namespace Api.Controllers.Models;

public class DoseEntity
{
    public DoseEntity()
    {
    }

    public DoseEntity(Guid doseId, string userId, DateTime takenAt, string medicationName, decimal doseAmountMg, bool deleted, string? notes = null)
    {
        DoseId = doseId;
        UserId = userId;
        TakenAt = takenAt;
        MedicationName = medicationName;
        DoseAmountMg = doseAmountMg;
        Deleted = deleted;
        Notes = notes;
    }

    public Guid DoseId { get; init; }

    public string UserId { get; init; } = null!;

    public DateTime TakenAt { get; init; }

    public bool Deleted { get; set; } = false;

    public string MedicationName { get; set; } = null!;

    public decimal DoseAmountMg { get; set; }

    public string? Notes { get; set; }
}
