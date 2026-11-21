using Api.Controllers.Levels;
using AwesomeAssertions;

namespace Tests.Levels;

public class NextDoseRulesTests
{
    private static readonly DateTime Start = new(2025, 1, 1, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void NextDueAt_IsLastDosePlusInterval()
    {
        NextDoseRules.NextDueAt(Start, intervalDays: 7).Should().Be(Start.AddDays(7));
    }

    [Fact]
    public void IsCurrent_WhenNotYetDue()
    {
        NextDoseRules.IsCurrent(nextDueAt: Start.AddDays(1), now: Start).Should().BeTrue();
    }

    [Fact]
    public void IsCurrent_WhenOverdueByExactly28Days()
    {
        NextDoseRules.IsCurrent(nextDueAt: Start, now: Start.AddDays(28)).Should().BeTrue();
    }

    [Fact]
    public void IsNotCurrent_WhenOverdueByMoreThan28Days()
    {
        NextDoseRules.IsCurrent(nextDueAt: Start, now: Start.AddDays(28).AddMinutes(1)).Should().BeFalse();
    }
}
