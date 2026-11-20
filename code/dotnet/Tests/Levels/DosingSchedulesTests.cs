using Api.Controllers.Levels;
using AwesomeAssertions;

namespace Tests.Levels;

public class DosingSchedulesTests
{
    private static readonly DateTime Start = new(2025, 1, 1, 8, 30, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Semaglutide", 7)]
    [InlineData("Tirzepatide", 5)]
    [InlineData("Retatrutide", 6)]
    public void Intervals_MatchTheHardcodedDefaults(string medicationName, double intervalDays)
    {
        DosingSchedules.All.Single(s => s.MedicationName == medicationName)
            .IntervalDays.Should().Be(intervalDays);
    }

    [Fact]
    public void Schedules_CoverEveryDrugPreset()
    {
        DosingSchedules.All.Select(s => s.MedicationName)
            .Should().BeEquivalentTo(DrugPresets.All.Select(d => d.Name));
    }

    [Fact]
    public void NextDueAt_IsLastDosePlusInterval()
    {
        DosingSchedules.NextDueAt(Start, new DosingSchedule("Test", IntervalDays: 7))
            .Should().Be(Start.AddDays(7));
    }

    [Fact]
    public void IsCurrent_WhenNotYetDue()
    {
        DosingSchedules.IsCurrent(nextDueAt: Start.AddDays(1), now: Start).Should().BeTrue();
    }

    [Fact]
    public void IsCurrent_WhenOverdueByExactly28Days()
    {
        DosingSchedules.IsCurrent(nextDueAt: Start, now: Start.AddDays(28)).Should().BeTrue();
    }

    [Fact]
    public void IsNotCurrent_WhenOverdueByMoreThan28Days()
    {
        DosingSchedules.IsCurrent(nextDueAt: Start, now: Start.AddDays(28).AddMinutes(1)).Should().BeFalse();
    }
}
