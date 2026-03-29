using System.Collections.Generic;

namespace ZeroDayOrbit.Core.Save;

/// <summary>
/// Serializable upgrade progression state.
/// </summary>
public sealed class UpgradeSaveData
{
    public List<string> AppliedUpgradeIds { get; set; } = [];
    public Dictionary<string, int> ApplicationCounts { get; set; } = new();
    public float StationWeight { get; set; }
}
