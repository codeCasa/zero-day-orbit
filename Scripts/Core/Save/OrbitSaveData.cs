namespace ZeroDayOrbit.Core.Save;

/// <summary>
/// Serializable orbit state.
/// </summary>
public sealed class OrbitSaveData
{
    public float OrbitDurationSeconds { get; set; }
    public float ElapsedOrbitTime { get; set; }
    public bool IsDaytime { get; set; }
    public float OrbitStability { get; set; }
}
