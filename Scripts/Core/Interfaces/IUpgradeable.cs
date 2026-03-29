using ZeroDayOrbit.Core.Models;

namespace ZeroDayOrbit.Core.Interfaces;

/// <summary>
/// Represents a system element that can receive upgrade effects.
/// </summary>
public interface IUpgradeable
{
    /// <summary>
    /// Applies upgrade modifiers to the implementing object.
    /// </summary>
    /// <param name="upgrade">Upgrade payload containing stat deltas and metadata.</param>
    void ApplyUpgrade(UpgradeData upgrade);
}
