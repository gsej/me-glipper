using Api.Controllers.Levels;
using AwesomeAssertions;

namespace Tests.Levels;

public class DrugLevelCalculatorTests
{
    // F = 1 so the arithmetic is easy to reason about in these tests.
    private static DrugParameters Drug(double halfLifeHours, double tmaxHours = 24) =>
        new("Test", halfLifeHours, Bioavailability: 1.0, tmaxHours);

    private const AbsorptionModel Instant = AbsorptionModel.Instant;

    private static readonly DateTime Start = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void InstantDecay_HalvesEachHalfLife()
    {
        var drug = Drug(halfLifeHours: 24);
        var doses = new[] { new DoseInput(Start, 1.0) };

        DrugLevelCalculator.AmountRemainingMg(doses, Start, drug, Instant).Should().BeApproximately(1.0, 1e-9);
        DrugLevelCalculator.AmountRemainingMg(doses, Start.AddHours(24), drug, Instant).Should().BeApproximately(0.5, 1e-9);
        DrugLevelCalculator.AmountRemainingMg(doses, Start.AddHours(48), drug, Instant).Should().BeApproximately(0.25, 1e-9);
    }

    [Theory]
    [InlineData(AbsorptionModel.Instant)]
    [InlineData(AbsorptionModel.FirstOrder)]
    public void FutureDoses_ContributeNothing(AbsorptionModel model)
    {
        var doses = new[] { new DoseInput(Start.AddHours(1), 1.0) };

        DrugLevelCalculator.AmountRemainingMg(doses, Start, Drug(24), model).Should().Be(0);
    }

    [Theory]
    [InlineData(AbsorptionModel.Instant)]
    [InlineData(AbsorptionModel.FirstOrder)]
    public void Doses_Superpose(AbsorptionModel model)
    {
        var at = Start.AddDays(9);
        var drug = Drug(100);
        var d1 = new DoseInput(Start, 1.0);
        var d2 = new DoseInput(Start.AddDays(4), 2.0);

        var combined = DrugLevelCalculator.AmountRemainingMg(new[] { d1, d2 }, at, drug, model);
        var separate = DrugLevelCalculator.AmountRemainingMg(new[] { d1 }, at, drug, model)
                     + DrugLevelCalculator.AmountRemainingMg(new[] { d2 }, at, drug, model);

        combined.Should().BeApproximately(separate, 1e-12);
    }

    [Fact]
    public void WeeklyDosing_ApproachesSteadyStateAccumulation()
    {
        // t½ = 168 h = the 1-week dosing interval, so e^(−k·τ) = 0.5 and the steady-state
        // accumulation ratio 1/(1 − e^(−k·τ)) = 2.0. After 20 weekly 1 mg doses the amount
        // immediately after the last dose should be ≈ 2.0 mg.
        var drug = Drug(halfLifeHours: 168);
        var doses = Enumerable.Range(0, 20).Select(i => new DoseInput(Start.AddDays(7 * i), 1.0)).ToArray();
        var lastDoseTime = Start.AddDays(7 * 19);

        DrugLevelCalculator.AmountRemainingMg(doses, lastDoseTime, drug, Instant).Should().BeApproximately(2.0, 0.01);
    }

    [Fact]
    public void EmptyDoses_ReturnZero()
    {
        DrugLevelCalculator.AmountRemainingMg(Array.Empty<DoseInput>(), Start, Drug(24)).Should().Be(0);
    }

    [Fact]
    public void Series_EvaluatesTheAmountAtEachTime()
    {
        var drug = Drug(halfLifeHours: 24);
        var doses = new[] { new DoseInput(Start, 1.0) };
        var times = new[] { Start, Start.AddHours(24), Start.AddHours(48) };

        var series = DrugLevelCalculator.AmountRemainingSeriesMg(doses, times, drug, Instant);

        series.Should().HaveCount(3);
        series[0].Should().BeApproximately(1.0, 1e-9);
        series[1].Should().BeApproximately(0.5, 1e-9);
        series[2].Should().BeApproximately(0.25, 1e-9);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidHalfLife_Throws(double halfLifeHours)
    {
        var act = () => DrugLevelCalculator.AmountRemainingMg(
            new[] { new DoseInput(Start, 1.0) }, Start, new DrugParameters("x", halfLifeHours, 1.0, 24));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.5)]
    public void InvalidBioavailability_Throws(double bioavailability)
    {
        var act = () => DrugLevelCalculator.AmountRemainingMg(
            new[] { new DoseInput(Start, 1.0) }, Start, new DrugParameters("x", 24, bioavailability, 12));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NegativeDoseAmount_Throws()
    {
        var act = () => DrugLevelCalculator.AmountRemainingMg(
            new[] { new DoseInput(Start, -1.0) }, Start.AddHours(1), Drug(24));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // --- First-order absorption (Bateman) ------------------------------------------------

    [Fact]
    public void FirstOrder_IsTheDefaultModel()
    {
        var drug = Drug(halfLifeHours: 165, tmaxHours: 48);
        var doses = new[] { new DoseInput(Start, 1.0) };
        var at = Start.AddHours(30);

        DrugLevelCalculator.AmountRemainingMg(doses, at, drug)
            .Should().Be(DrugLevelCalculator.AmountRemainingMg(doses, at, drug, AbsorptionModel.FirstOrder));
    }

    [Fact]
    public void FirstOrder_SingleDose_StartsAtZeroPeaksNearTmaxThenFalls()
    {
        var drug = Drug(halfLifeHours: 165, tmaxHours: 48);
        var doses = new[] { new DoseInput(Start, 1.0) };
        double At(double hours) => DrugLevelCalculator.AmountRemainingMg(doses, Start.AddHours(hours), drug);

        At(0).Should().BeApproximately(0, 1e-12);
        At(24).Should().BeLessThan(At(48));
        At(48).Should().BeGreaterThan(At(72));
        At(10 * 165).Should().BeLessThan(0.002); // ~10 half-lives on, close to 0
    }

    [Fact]
    public void FirstOrder_ApproachesInstantDecay_AtLongTimes()
    {
        // Once absorption is complete the Bateman curve decays like instant decay, scaled by
        // ka/(ka − k) (the extra drug that was eliminated more slowly while still absorbing).
        var drug = Drug(halfLifeHours: 165, tmaxHours: 48);
        var doses = new[] { new DoseInput(Start, 1.0) };
        var k = Math.Log(2) / 165;
        var ka = DrugLevelCalculator.AbsorptionRateFromTmax(165, 48);
        var at = Start.AddHours(1000);

        var firstOrder = DrugLevelCalculator.AmountRemainingMg(doses, at, drug);
        var instant = DrugLevelCalculator.AmountRemainingMg(doses, at, drug, Instant);

        (firstOrder / instant).Should().BeApproximately(ka / (ka - k), 1e-6);
    }

    [Fact]
    public void Bateman_IsContinuousAcrossTheNearlyEqualRatesSwitchover()
    {
        // Either side of |ka − k| = 1e-9 the general formula and the limiting form must agree.
        var k = Math.Log(2) / 100;
        const double t = 50;
        var limit = 1.0 * 2.0 * k * t * Math.Exp(-k * t);

        foreach (var ka in new[] { k - 2e-9, k - 5e-10, k, k + 5e-10, k + 2e-9 })
            DrugLevelCalculator.BatemanAmountMg(2.0, t, k, ka, 1.0).Should().BeApproximately(limit, 1e-6);
    }

    [Theory]
    [InlineData(165, 48)]
    [InlineData(117, 24)]
    [InlineData(144, 36)]
    [InlineData(24, 2)]
    public void AbsorptionRateFromTmax_RoundTripsToTheSingleDosePeak(double halfLifeHours, double tmaxHours)
    {
        var k = Math.Log(2) / halfLifeHours;
        var ka = DrugLevelCalculator.AbsorptionRateFromTmax(halfLifeHours, tmaxHours);
        ka.Should().BeGreaterThan(k);

        // Locate the peak numerically on a 0.01 h grid.
        var peakHours = 0.0;
        var peak = double.MinValue;
        for (var t = 0.0; t <= 3 * tmaxHours; t += 0.01)
        {
            var amount = DrugLevelCalculator.BatemanAmountMg(1.0, t, k, ka, 1.0);
            if (amount > peak)
            {
                peak = amount;
                peakHours = t;
            }
        }

        peakHours.Should().BeApproximately(tmaxHours, 0.1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(239)] // ≥ t½/ln 2 (= 238.04 h for t½ = 165 h) has no ka > k solution
    public void InvalidTmax_Throws(double tmaxHours)
    {
        var act = () => DrugLevelCalculator.AmountRemainingMg(
            new[] { new DoseInput(Start, 1.0) }, Start.AddHours(1), new DrugParameters("x", 165, 1.0, tmaxHours));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void InvalidTmax_IsIgnoredByTheInstantModel()
    {
        var act = () => DrugLevelCalculator.AmountRemainingMg(
            new[] { new DoseInput(Start, 1.0) }, Start.AddHours(1), new DrugParameters("x", 165, 1.0, 0), Instant);

        act.Should().NotThrow();
    }
}
