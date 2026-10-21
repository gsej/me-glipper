namespace Api.Controllers.Levels;

/// <summary>
/// Estimates how much of a drug remains in the body, given a dose history and the moment
/// of interest. Pure: no I/O and no clock access — the caller passes <paramref name="at"/>.
///
/// Step 1 uses the simplest model: instant absorption (the whole dose is in the body
/// immediately) with first-order elimination, i.e. A(t) = F·D·e^(−k·t) per dose, summed
/// over every dose already given (superposition — the model is linear, so the sum is exact
/// within it). A later step adds first-order absorption (the Bateman function) by replacing
/// <see cref="AmountFromSingleDose"/>; nothing else here needs to change.
///
/// The result is milligrams of drug in the body — NOT a blood concentration (that would need
/// a volume of distribution, deliberately left out for now). This is an educational
/// population-average model, not a clinical tool.
/// </summary>
public static class DrugLevelCalculator
{
    public static double AmountRemainingMg(IEnumerable<DoseInput> doses, DateTime at, DrugParameters drug)
    {
        if (drug.HalfLifeHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(drug), "Half-life must be greater than 0.");
        if (drug.Bioavailability <= 0 || drug.Bioavailability > 1)
            throw new ArgumentOutOfRangeException(nameof(drug), "Bioavailability must be in the range (0, 1].");

        var k = Math.Log(2) / drug.HalfLifeHours; // elimination rate constant (per hour)

        double total = 0;
        foreach (var dose in doses)
        {
            if (dose.AmountMg < 0)
                throw new ArgumentOutOfRangeException(nameof(doses), "Dose amounts must be 0 or greater.");

            var elapsedHours = (at - dose.TakenAt).TotalHours;
            if (elapsedHours < 0)
                continue; // dose is in the future relative to `at`; contributes nothing

            total += AmountFromSingleDose(dose.AmountMg, elapsedHours, k, drug.Bioavailability);
        }

        return total;
    }

    /// <summary>
    /// Amount remaining from a single dose, <paramref name="elapsedHours"/> after it was given.
    /// Instant absorption + first-order elimination: A = F·D·e^(−k·t). Isolated so a different
    /// absorption model (e.g. the Bateman function) can replace just this method later.
    /// </summary>
    private static double AmountFromSingleDose(double doseMg, double elapsedHours, double k, double bioavailability)
        => bioavailability * doseMg * Math.Exp(-k * elapsedHours);
}
