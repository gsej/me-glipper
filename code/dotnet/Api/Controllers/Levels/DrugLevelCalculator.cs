namespace Api.Controllers.Levels;

/// <summary>
/// Estimates how much of a drug remains in the body, given a dose history and the moment
/// of interest. Pure: no I/O and no clock access — the caller passes <c>at</c>.
///
/// One-compartment model with first-order elimination. Each dose is computed on its own and
/// the results are summed (superposition — the model is linear, so the sum is exact within it).
/// Two absorption models (<see cref="AbsorptionModel"/>):
/// <list type="bullet">
/// <item><b>Instant</b>: the whole dose is in the body immediately, A(t) = F·D·e^(−k·t).</item>
/// <item><b>FirstOrder</b> (the Bateman function): drug moves from the injection site into the
/// body at rate ka, A(t) = F·D·ka/(ka − k)·(e^(−k·t) − e^(−ka·t)). It rises from zero to a peak
/// at Tmax, then decays, behaving like instant decay at long times.</item>
/// </list>
///
/// The result is milligrams of drug in the body — NOT a blood concentration (that would need
/// a volume of distribution, deliberately left out for now). This is an educational
/// population-average model, not a clinical tool: real individuals vary, and for retatrutide
/// the parameters come from early trial data only.
/// </summary>
public static class DrugLevelCalculator
{
    // Below this |ka − k| the Bateman formula divides by ~zero, so its limiting form is used.
    private const double NearlyEqualRates = 1e-9;

    public static double AmountRemainingMg(
        IEnumerable<DoseInput> doses, DateTime at, DrugParameters drug,
        AbsorptionModel model = AbsorptionModel.FirstOrder)
        => Total(doses, at, SingleDoseFunction(drug, model));

    /// <summary>
    /// Evaluates <see cref="AmountRemainingMg"/> at each of <paramref name="times"/>, producing a
    /// series for charting. The parameters are validated (and ka derived) once, not per point.
    /// </summary>
    public static IReadOnlyList<double> AmountRemainingSeriesMg(
        IReadOnlyList<DoseInput> doses, IEnumerable<DateTime> times, DrugParameters drug,
        AbsorptionModel model = AbsorptionModel.FirstOrder)
    {
        var single = SingleDoseFunction(drug, model);
        return times.Select(at => Total(doses, at, single)).ToList();
    }

    /// <summary>
    /// Amount in the body <paramref name="elapsedHours"/> after a single dose, using the Bateman
    /// function: F·D·ka/(ka − k)·(e^(−k·t) − e^(−ka·t)). When ka and k are (nearly) equal that
    /// divides by ~zero, so the limiting form F·D·k·t·e^(−k·t) is used instead.
    /// </summary>
    public static double BatemanAmountMg(
        double doseMg, double elapsedHours, double eliminationRate, double absorptionRate, double bioavailability)
    {
        var k = eliminationRate;
        var ka = absorptionRate;
        var t = elapsedHours;

        if (Math.Abs(ka - k) < NearlyEqualRates)
            return bioavailability * doseMg * k * t * Math.Exp(-k * t);

        return bioavailability * doseMg * ka / (ka - k) * (Math.Exp(-k * t) - Math.Exp(-ka * t));
    }

    /// <summary>
    /// Derives the absorption rate constant ka (per hour) from the elimination half-life and the
    /// time to peak after a single dose, by solving Tmax = ln(ka/k)/(ka − k) for the root with
    /// ka &gt; k (bisection; Tmax falls monotonically as ka rises). That root exists only when
    /// Tmax &lt; 1/k = t½/ln 2 — beyond that, ka would have to be slower than elimination.
    /// </summary>
    public static double AbsorptionRateFromTmax(double halfLifeHours, double tmaxHours)
    {
        if (halfLifeHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(halfLifeHours), "Half-life must be greater than 0.");
        if (tmaxHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(tmaxHours), "Tmax must be greater than 0.");

        var k = Math.Log(2) / halfLifeHours;
        if (tmaxHours >= 1 / k)
            throw new ArgumentOutOfRangeException(nameof(tmaxHours),
                $"Tmax must be less than t½/ln 2 ({1 / k:F1} h) for a half-life of {halfLifeHours} h.");

        // Bracket the root: Tmax(lo) > target (as ka → k, Tmax → 1/k); grow hi until Tmax(hi) < target.
        var lo = k;
        var hi = 2 * k;
        while (TmaxFor(hi, k) > tmaxHours)
            hi *= 2;

        for (var i = 0; i < 200 && hi - lo > 1e-15 * hi; i++)
        {
            var mid = (lo + hi) / 2;
            if (TmaxFor(mid, k) > tmaxHours)
                lo = mid;
            else
                hi = mid;
        }

        return (lo + hi) / 2;
    }

    private static double TmaxFor(double ka, double k)
        => Math.Abs(ka - k) < NearlyEqualRates ? 1 / k : Math.Log(ka / k) / (ka - k);

    private static double Total(IEnumerable<DoseInput> doses, DateTime at, Func<double, double, double> single)
    {
        double total = 0;
        foreach (var dose in doses)
        {
            if (dose.AmountMg < 0)
                throw new ArgumentOutOfRangeException(nameof(doses), "Dose amounts must be 0 or greater.");

            var elapsedHours = (at - dose.TakenAt).TotalHours;
            if (elapsedHours < 0)
                continue; // dose is in the future relative to `at`; contributes nothing

            total += single(dose.AmountMg, elapsedHours);
        }

        return total;
    }

    /// <summary>
    /// Validates the parameters and returns the per-dose function (dose mg, hours since dose)
    /// → mg in the body, for the chosen absorption model.
    /// </summary>
    private static Func<double, double, double> SingleDoseFunction(DrugParameters drug, AbsorptionModel model)
    {
        if (drug.HalfLifeHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(drug), "Half-life must be greater than 0.");
        if (drug.Bioavailability <= 0 || drug.Bioavailability > 1)
            throw new ArgumentOutOfRangeException(nameof(drug), "Bioavailability must be in the range (0, 1].");

        var k = Math.Log(2) / drug.HalfLifeHours; // elimination rate constant (per hour)
        var f = drug.Bioavailability;

        switch (model)
        {
            case AbsorptionModel.Instant:
                return (doseMg, t) => f * doseMg * Math.Exp(-k * t);

            case AbsorptionModel.FirstOrder:
                var ka = AbsorptionRateFromTmax(drug.HalfLifeHours, drug.TmaxHours);
                return (doseMg, t) => BatemanAmountMg(doseMg, t, k, ka, f);

            default:
                throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown absorption model.");
        }
    }
}
