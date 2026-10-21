namespace Api.Controllers.Levels;

/// <summary>
/// Built-in population-average presets. Step 1 ships semaglutide only; tirzepatide and
/// retatrutide follow with the Bateman model. These are population averages (and some are
/// uncertain), so every value is overridable via <see cref="DrugParameters"/>.
/// </summary>
public static class DrugPresets
{
    // Semaglutide (subcutaneous): t½ ≈ 165 h (~7 days), F ≈ 0.89.
    // Sources: FDA prescribing information for Ozempic/Wegovy, §12.3 Pharmacokinetics;
    // Carlsson Petri et al., "Semaglutide s.c. once-weekly in type 2 diabetes: a population
    // pharmacokinetic analysis", Diabetes Therapy 2018;9(4):1533–1547.
    public static readonly DrugParameters Semaglutide = new(
        Name: "Semaglutide",
        HalfLifeHours: 165,
        Bioavailability: 0.89);
}
