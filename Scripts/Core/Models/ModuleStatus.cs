namespace ZeroDayOrbit.Core.Models;

/// <summary>
/// Lightweight status payload for one system module.
/// </summary>
public sealed class ModuleStatus
{
    /// <summary>
    /// Gets or sets module display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the module is online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets module criticality classification.
    /// </summary>
    public SystemCriticality Criticality { get; set; }

    /// <summary>
    /// Gets or sets whether this module is currently in a failed state.
    /// </summary>
    public bool IsFailed { get; set; }

    /// <summary>
    /// Gets or sets whether this module is manually disabled.
    /// </summary>
    public bool IsManuallyDisabled { get; set; }

    /// <summary>
    /// Gets or sets whether the module was powered during the latest tick.
    /// </summary>
    public bool IsPowered { get; set; }

    /// <summary>
    /// Gets or sets module efficiency multiplier.
    /// </summary>
    public float Efficiency { get; set; }

    /// <summary>
    /// Gets or sets module power draw.
    /// </summary>
    public float PowerDraw { get; set; }

    /// <summary>
    /// Gets or sets normalized health percentage.
    /// </summary>
    public float HealthPercent { get; set; }

    /// <summary>
    /// Gets or sets optional detail text for debug display.
    /// </summary>
    public string Detail { get; set; } = string.Empty;
}
