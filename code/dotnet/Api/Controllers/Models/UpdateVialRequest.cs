using System.ComponentModel.DataAnnotations;

namespace Api.Controllers.Models;

/// <summary>
/// The contents of a vial (both amounts must be greater than 0) and the date it was started.
/// StartedOn is nullable only so that leaving it out is a 400 rather than a default date.
/// </summary>
public record UpdateVialRequest(
    [Range(0, double.MaxValue, MinimumIsExclusive = true)] decimal InitialAmountMg,
    [Range(0, double.MaxValue, MinimumIsExclusive = true)] decimal InitialVolumeMl,
    [Required] DateOnly? StartedOn);
