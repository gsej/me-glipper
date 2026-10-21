namespace Api.Controllers.Levels;

/// <summary>
/// A single administered dose reduced to what the PK model needs — when it was given and
/// how much. Deliberately decoupled from the Dose domain entity so the calculator stays
/// pure and easy to unit-test.
/// </summary>
public readonly record struct DoseInput(DateTime TakenAt, double AmountMg);
