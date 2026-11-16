namespace Api.Controllers.Levels;

/// <summary>How a dose enters the body in the PK model.</summary>
public enum AbsorptionModel
{
    /// <summary>The whole dose is in the body immediately: A(t) = F·D·e^(−k·t).</summary>
    Instant,

    /// <summary>
    /// First-order absorption from the injection site (the Bateman function): each dose rises
    /// from zero to a peak at Tmax, then decays.
    /// </summary>
    FirstOrder,
}
