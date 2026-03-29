namespace ZeroDayOrbit.Core.Save;

/// <summary>
/// Lightweight metadata used to list and identify save slots.
/// </summary>
public sealed class SaveMetadata
{
    public string SlotId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SavedAtUtc { get; set; } = string.Empty;
    public string GameState { get; set; } = string.Empty;
    public float BatteryPercent { get; set; }
    public float OrbitProgressPercent { get; set; }
    public float OrbitStability { get; set; }
    public string FilePath { get; set; } = string.Empty;
}
