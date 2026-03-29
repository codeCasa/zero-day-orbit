using ZeroDayOrbit.Core.Models;

namespace ZeroDayOrbit.Core.Interfaces;

/// <summary>
/// Defines the contract for a station subsystem that can be simulated every frame.
/// </summary>
public interface ISystemModule
{
    /// <summary>
    /// Gets the user-facing display name for the module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the baseline power usage in units per second while the module is online.
    /// </summary>
    float PowerDraw { get; }

    /// <summary>
    /// Gets the normalized effectiveness multiplier for module behavior.
    /// </summary>
    float Efficiency { get; }

    /// <summary>
    /// Gets a value indicating whether this module is currently operating.
    /// </summary>
    bool IsOnline { get; }

    /// <summary>
    /// Gets normalized module health percentage in range 0-100.
    /// </summary>
    float HealthPercent { get; }

    /// <summary>
    /// Gets module criticality classification used for failure consequence routing.
    /// </summary>
    SystemCriticality Criticality { get; }

    /// <summary>
    /// Gets a value indicating whether the module is intentionally disabled by player/system logic.
    /// </summary>
    bool IsManuallyDisabled { get; }

    /// <summary>
    /// Gets a value indicating whether module health/condition has crossed into a failed state.
    /// </summary>
    bool IsFailed { get; }

    /// <summary>
    /// Advances the module simulation by a time slice.
    /// </summary>
    /// <param name="delta">Elapsed simulation time in seconds.</param>
    /// <param name="isPowered">Whether the module has sufficient power this frame.</param>
    void Tick(float delta, bool isPowered);

    /// <summary>
    /// Changes whether this module is online and participating in active behavior.
    /// </summary>
    /// <param name="online">True to enable module behavior; false to disable it.</param>
    void SetOnline(bool online);

    /// <summary>
    /// Sets whether this module is intentionally disabled without implying permanent failure.
    /// </summary>
    /// <param name="disabled">True to disable the module, false to re-enable it.</param>
    void SetManuallyDisabled(bool disabled);
}
