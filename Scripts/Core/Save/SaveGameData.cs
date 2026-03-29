using System.Collections.Generic;

namespace ZeroDayOrbit.Core.Save;

/// <summary>
/// Root persisted save state for one run.
/// </summary>
public sealed class SaveGameData
{
    public SaveMetadata Metadata { get; set; } = new();
    public string GameState { get; set; } = string.Empty;
    public string GameOverReason { get; set; } = string.Empty;
    public string GameOverMessage { get; set; } = string.Empty;
    public bool IsGameRunning { get; set; }
    public float SessionElapsedSeconds { get; set; }
    public float PendingDemandPenaltyMultiplier { get; set; } = 1f;
    public float PowerFailureTimer { get; set; }
    public OrbitSaveData Orbit { get; set; } = new();
    public List<ModuleSaveData> Modules { get; set; } = [];
    public UpgradeSaveData Upgrades { get; set; } = new();
}
