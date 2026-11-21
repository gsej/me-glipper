namespace Api.Controllers.Levels;

/// <summary>
/// Population-average pharmacokinetic parameters for one drug, as the calculator takes them.
/// The values come from the Medications table (see docs/medications.md for their sources;
/// they are population averages and some are uncertain). <see cref="TmaxHours"/> (time to
/// peak after a single dose) is only used by the first-order absorption model; the absorption
/// rate ka is derived from it (<see cref="DrugLevelCalculator.AbsorptionRateFromTmax"/>), since
/// published sources give Tmax rather than ka.
/// </summary>
public record DrugParameters(string Name, double HalfLifeHours, double Bioavailability, double TmaxHours);
