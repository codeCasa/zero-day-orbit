namespace ZeroDayOrbit.Core.Models;

/// <summary>
/// Describes why the current run ended.
/// </summary>
public enum GameOverReason
{
    /// <summary>
    /// No game-over condition has been triggered.
    /// </summary>
    None,

    /// <summary>
    /// Station power could not be sustained.
    /// </summary>
    PowerFailure,

    /// <summary>
    /// Life support conditions became unrecoverable.
    /// </summary>
    LifeSupportFailure,

    /// <summary>
    /// Temperature remained unsafe for too long.
    /// </summary>
    ThermalFailure,

    /// <summary>
    /// Orbit stability collapsed.
    /// </summary>
    OrbitFailure
}
