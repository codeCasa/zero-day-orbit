namespace ZeroDayOrbit.Core.Models;

/// <summary>
/// Snapshot of orbit timing and stability values.
/// </summary>
public sealed class OrbitStatusSnapshot
{
    /// <summary>
    /// Gets or sets elapsed time in current orbit cycle.
    /// </summary>
    public float ElapsedOrbitTime { get; set; }

    /// <summary>
    /// Gets or sets normalized orbit progress in range 0-1.
    /// </summary>
    public float NormalizedOrbitProgress { get; set; }

    /// <summary>
    /// Gets or sets whether station is in daylight phase.
    /// </summary>
    public bool IsDaytime { get; set; }

    /// <summary>
    /// Gets or sets whether station is in night phase.
    /// </summary>
    public bool IsNighttime { get; set; }

    /// <summary>
    /// Gets or sets orbit stability percentage.
    /// </summary>
    public float OrbitStability { get; set; }
}
