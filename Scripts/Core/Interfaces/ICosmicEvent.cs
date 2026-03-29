using ZeroDayOrbit.Core.Managers;

namespace ZeroDayOrbit.Core.Interfaces;

/// <summary>
/// Defines an event that can modify the station state through the system manager.
/// </summary>
public interface ICosmicEvent
{
    /// <summary>
    /// Gets the display name of the event.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies event effects to station systems.
    /// </summary>
    /// <param name="systemManager">System manager providing access to active modules.</param>
    void Apply(SystemManager systemManager);
}
