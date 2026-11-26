namespace Api.Controllers.Models;

/// <summary>A user's current vial of one medication.</summary>
public class VialEntity
{
    public VialEntity()
    {
    }

    public VialEntity(Guid vialId, string userId, int medicationId, decimal initialAmountMg, decimal initialVolumeMl, DateOnly startedOn)
    {
        VialId = vialId;
        UserId = userId;
        MedicationId = medicationId;
        InitialAmountMg = initialAmountMg;
        InitialVolumeMl = initialVolumeMl;
        StartedOn = startedOn;
    }

    public Guid VialId { get; init; }

    public string UserId { get; init; } = null!;

    public int MedicationId { get; init; }

    /// <summary>
    /// The medication's name, as stored in Medications. Filled in by the repository when a vial
    /// is read; not written (vials refer to their medication by <see cref="MedicationId"/>).
    /// </summary>
    public string MedicationName { get; set; } = "";

    /// <summary>How much medication the vial was made up with, in mg.</summary>
    public decimal InitialAmountMg { get; set; }

    /// <summary>How much liquid the vial was made up with, in ml.</summary>
    public decimal InitialVolumeMl { get; set; }

    /// <summary>The calendar date the vial was started (no time or timezone).</summary>
    public DateOnly StartedOn { get; set; }
}
