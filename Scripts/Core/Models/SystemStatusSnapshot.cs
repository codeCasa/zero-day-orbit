using System.Collections.Generic;

namespace ZeroDayOrbit.Core.Models;

/// <summary>
/// Snapshot of whole-station simulation values for UI and debug output.
/// </summary>
public sealed class SystemStatusSnapshot
{
    /// <summary>
    /// Gets or sets total active power demand excluding power module internal draw.
    /// </summary>
    public float ConsumerPowerDemand { get; set; }

    /// <summary>
    /// Gets or sets total station power draw including power module draw.
    /// </summary>
    public float TotalPowerDraw { get; set; }

    /// <summary>
    /// Gets or sets current solar generation rate for this frame.
    /// </summary>
    public float SolarGeneration { get; set; }

    /// <summary>
    /// Gets or sets net power balance. Positive means charging, negative means discharging.
    /// </summary>
    public float NetPowerBalance { get; set; }

    /// <summary>
    /// Gets or sets battery percentage from 0 to 100.
    /// </summary>
    public float BatteryPercent { get; set; }

    /// <summary>
    /// Gets or sets whether the station is currently in a power deficit.
    /// </summary>
    public bool IsPowerDeficit { get; set; }

    /// <summary>
    /// Gets module-level status entries.
    /// </summary>
    public List<ModuleStatus> Modules { get; set; } = new();
}
