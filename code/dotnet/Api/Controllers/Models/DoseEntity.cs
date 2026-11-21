namespace Api.Controllers.Models;

public class DoseEntity
{
    public DoseEntity()
    {
    }

    public DoseEntity(Guid doseId, string userId, DateTime takenAt, int medicationId, decimal doseAmountMg, bool deleted, string? notes = null)
    {
        DoseId = doseId;
        UserId = userId;
        TakenAt = takenAt;
        MedicationId = medicationId;
        DoseAmountMg = doseAmountMg;
        Deleted = deleted;
        Notes = notes;
    }

    public Guid DoseId { get; init; }

    public string UserId { get; init; } = null!;

    public DateTime TakenAt { get; init; }

    public bool Deleted { get; set; } = false;

    public int MedicationId { get; set; }

    /// <summary>
    /// The medication's name, as stored in Medications. Filled in by the repository when a dose
    /// is read; not written (doses refer to their medication by <see cref="MedicationId"/>).
    /// </summary>
    public string MedicationName { get; set; } = "";

    public decimal DoseAmountMg { get; set; }

    public string? Notes { get; set; }
}
