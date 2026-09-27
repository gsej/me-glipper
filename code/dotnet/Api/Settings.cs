namespace Api;

public record Settings
{
    public int AverageWeightWindowInDays { get; init; } = 7;
}