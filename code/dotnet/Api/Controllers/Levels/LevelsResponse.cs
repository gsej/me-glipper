namespace Api.Controllers.Levels;

/// <summary>Estimated amount of each tracked drug currently in the body.</summary>
public record LevelsResponse(IEnumerable<DrugLevel> Levels);

public record DrugLevel(string MedicationName, double AmountRemainingMg);
