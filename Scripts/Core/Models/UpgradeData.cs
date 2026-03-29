namespace ZeroDayOrbit.Core.Models;

/// <summary>
/// Data transfer object describing a single upgrade and its stat effects.
/// </summary>
public sealed class UpgradeData
{
    /// <summary>
    /// Gets or sets the unique identifier used for save/load and lookup.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user-facing upgrade name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a short description of upgrade behavior.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the weight added to the station after installation.
    /// </summary>
    public float Weight { get; set; }

    /// <summary>
    /// Gets or sets the additive change to power usage.
    /// Negative values reduce draw.
    /// </summary>
    public float PowerUsageDelta { get; set; }

    /// <summary>
    /// Gets or sets the additive efficiency bonus applied to target systems.
    /// </summary>
    public float EfficiencyBonus { get; set; }

    /// <summary>
    /// Gets or sets the additive battery capacity change, used by power systems.
    /// </summary>
    public float BatteryCapacityDelta { get; set; }

    /// <summary>
    /// Gets or sets the additive heat dissipation change, used by thermal systems.
    /// </summary>
    public float HeatDissipationBonus { get; set; }

    /// <summary>
    /// Gets or sets direct orbit stability bonus applied when relevant.
    /// </summary>
    public float OrbitStabilityBonus { get; set; }

    /// <summary>
    /// Gets or sets resource cost for future economy integration.
    /// </summary>
    public int Cost { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this upgrade can be applied more than once.
    /// </summary>
    public bool IsRepeatable { get; set; }

    /// <summary>
    /// Gets or sets maximum allowed applications. Zero or less means unlimited when repeatable.
    /// </summary>
    public int MaxApplications { get; set; } = 1;

    /// <summary>
    /// Gets or sets prerequisite upgrade IDs that must be applied first.
    /// </summary>
    public string[] PrerequisiteUpgradeIds { get; set; } = [];

    /// <summary>
    /// Gets or sets optional category key for grouping in UI.
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Gets or sets optional icon key for future UI presentation.
    /// </summary>
    public string IconKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this upgrade is enabled in current content set.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets which subsystem category receives this upgrade.
    /// </summary>
    public UpgradeTargetType TargetType { get; set; }
}
