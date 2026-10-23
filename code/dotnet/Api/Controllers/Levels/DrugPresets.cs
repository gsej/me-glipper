namespace Api.Controllers.Levels;

/// <summary>
/// Built-in population-average presets for the tracked GLP-1 drugs. These are population
/// averages (and some are uncertain), so every value is overridable via
/// <see cref="DrugParameters"/>. The instant-decay model only needs half-life and
/// bioavailability; absorption (Tmax/ka) will be added with the Bateman model.
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

    // Tirzepatide (subcutaneous): t½ ≈ 117 h (~5 days), F ≈ 0.80.
    // Sources: FDA prescribing information for Mounjaro/Zepbound, §12.3 Pharmacokinetics;
    // Schneck & Urva, "Population pharmacokinetics of the GIP/GLP receptor agonist
    // tirzepatide", CPT: Pharmacometrics & Systems Pharmacology 2024;13(3):494–503.
    public static readonly DrugParameters Tirzepatide = new(
        Name: "Tirzepatide",
        HalfLifeHours: 117,
        Bioavailability: 0.80);

    // Retatrutide (subcutaneous): t½ ≈ 144 h (~6 days), F ≈ 1.0 (not well published —
    // defaulted to 1). Parameters are approximate: early trial data only.
    // Sources: Urva et al., Lancet 2022;400(10366):1869–1881 (phase 1b MAD trial);
    // Jastreboff et al., NEJM 2023;389(6):514–526 (PK in the supplementary appendix).
    public static readonly DrugParameters Retatrutide = new(
        Name: "Retatrutide",
        HalfLifeHours: 144,
        Bioavailability: 1.0);

    /// <summary>All presets, in display order. Declared last so the fields above are
    /// initialised first.</summary>
    public static readonly IReadOnlyList<DrugParameters> All = new[]
    {
        Semaglutide,
        Tirzepatide,
        Retatrutide,
    };
}
