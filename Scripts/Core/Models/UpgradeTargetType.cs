namespace ZeroDayOrbit.Core.Models;

/// <summary>
/// Identifies which system category an upgrade should affect.
/// </summary>
public enum UpgradeTargetType
{
    /// <summary>
    /// Targets the station power generation and storage stack.
    /// </summary>
    PowerSystem,

    /// <summary>
    /// Targets oxygen and atmospheric processing systems.
    /// </summary>
    LifeSupport,

    /// <summary>
    /// Targets station thermal regulation systems.
    /// </summary>
    Heat,

    /// <summary>
    /// Targets navigation and trajectory control systems.
    /// </summary>
    Navigation,

    /// <summary>
    /// Targets communications systems.
    /// </summary>
    Communications,

    /// <summary>
    /// Applies to all upgradeable station modules.
    /// </summary>
    StationGlobal
}
