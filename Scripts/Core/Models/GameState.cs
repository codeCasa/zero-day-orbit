namespace ZeroDayOrbit.Core.Models;

/// <summary>
/// Represents high-level game progression state.
/// </summary>
public enum GameState
{
    /// <summary>
    /// Active gameplay simulation is running.
    /// </summary>
    Running,

    /// <summary>
    /// The run ended due to a failure condition.
    /// </summary>
    GameOver,

    /// <summary>
    /// Reserved for future win-state implementation.
    /// </summary>
    Victory
}
