using Api.Controllers.Levels;
using AwesomeAssertions;

namespace Tests.Levels;

public class DrugLevelCalculatorTests
{
    // F = 1 so the arithmetic is easy to reason about in these tests.
    private static DrugParameters Drug(double halfLifeHours) =>
        new("Test", halfLifeHours, Bioavailability: 1.0);

    private static readonly DateTime Start = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void InstantDecay_HalvesEachHalfLife()
    {
        var drug = Drug(halfLifeHours: 24);
        var doses = new[] { new DoseInput(Start, 1.0) };

        DrugLevelCalculator.AmountRemainingMg(doses, Start, drug).Should().BeApproximately(1.0, 1e-9);
        DrugLevelCalculator.AmountRemainingMg(doses, Start.AddHours(24), drug).Should().BeApproximately(0.5, 1e-9);
        DrugLevelCalculator.AmountRemainingMg(doses, Start.AddHours(48), drug).Should().BeApproximately(0.25, 1e-9);
    }

    [Fact]
    public void FutureDoses_ContributeNothing()
    {
        var doses = new[] { new DoseInput(Start.AddHours(1), 1.0) };

        DrugLevelCalculator.AmountRemainingMg(doses, Start, Drug(24)).Should().Be(0);
    }

    [Fact]
    public void Doses_Superpose()
    {
        var at = Start.AddDays(9);
        var drug = Drug(100);
        var d1 = new DoseInput(Start, 1.0);
        var d2 = new DoseInput(Start.AddDays(4), 2.0);

        var combined = DrugLevelCalculator.AmountRemainingMg(new[] { d1, d2 }, at, drug);
        var separate = DrugLevelCalculator.AmountRemainingMg(new[] { d1 }, at, drug)
                     + DrugLevelCalculator.AmountRemainingMg(new[] { d2 }, at, drug);

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

        DrugLevelCalculator.AmountRemainingMg(doses, lastDoseTime, drug).Should().BeApproximately(2.0, 0.01);
    }

    [Fact]
    public void EmptyDoses_ReturnZero()
    {
        DrugLevelCalculator.AmountRemainingMg(Array.Empty<DoseInput>(), Start, Drug(24)).Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidHalfLife_Throws(double halfLifeHours)
    {
        var act = () => DrugLevelCalculator.AmountRemainingMg(
            new[] { new DoseInput(Start, 1.0) }, Start, new DrugParameters("x", halfLifeHours, 1.0));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.5)]
    public void InvalidBioavailability_Throws(double bioavailability)
    {
        var act = () => DrugLevelCalculator.AmountRemainingMg(
            new[] { new DoseInput(Start, 1.0) }, Start, new DrugParameters("x", 24, bioavailability));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NegativeDoseAmount_Throws()
    {
        var act = () => DrugLevelCalculator.AmountRemainingMg(
            new[] { new DoseInput(Start, -1.0) }, Start.AddHours(1), Drug(24));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SemaglutidePreset_HasExpectedParameters()
    {
        DrugPresets.Semaglutide.Name.Should().Be("Semaglutide");
        DrugPresets.Semaglutide.HalfLifeHours.Should().Be(165);
        DrugPresets.Semaglutide.Bioavailability.Should().Be(0.89);
    }

    [Fact]
    public void TirzepatidePreset_HasExpectedParameters()
    {
        DrugPresets.Tirzepatide.Name.Should().Be("Tirzepatide");
        DrugPresets.Tirzepatide.HalfLifeHours.Should().Be(117);
        DrugPresets.Tirzepatide.Bioavailability.Should().Be(0.80);
    }

    [Fact]
    public void RetatrutidePreset_HasExpectedParameters()
    {
        DrugPresets.Retatrutide.Name.Should().Be("Retatrutide");
        DrugPresets.Retatrutide.HalfLifeHours.Should().Be(144);
        DrugPresets.Retatrutide.Bioavailability.Should().Be(1.0);
    }

    [Fact]
    public void All_ListsTheThreePresetsInOrder()
    {
        DrugPresets.All.Select(d => d.Name)
            .Should().Equal("Semaglutide", "Tirzepatide", "Retatrutide");
    }
}
