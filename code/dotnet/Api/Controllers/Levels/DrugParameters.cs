namespace Api.Controllers.Levels;

/// <summary>
/// Population-average pharmacokinetic parameters for one drug. Values are overridable
/// (see <see cref="DrugPresets"/>) because they are population averages and some are
/// uncertain. Step 1 models instant absorption, so only the elimination half-life and
/// bioavailability are needed; an absorption rate (ka) will be added with the Bateman model.
/// </summary>
public record DrugParameters(string Name, double HalfLifeHours, double Bioavailability);
