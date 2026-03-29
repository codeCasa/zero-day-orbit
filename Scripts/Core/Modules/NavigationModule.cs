using Godot;
using ZeroDayOrbit.Core.Interfaces;
using ZeroDayOrbit.Core.Models;

namespace ZeroDayOrbit.Core.Modules;

/// <summary>
/// Simulates station navigation health, fuel consumption, and course correction quality.
/// </summary>
public sealed class NavigationModule : ISystemModule, IUpgradeable
{
    /// <inheritdoc />
    public string Name => "Navigation";

    /// <inheritdoc />
    public float PowerDraw { get; private set; } = 2.2f;

    /// <inheritdoc />
    public float Efficiency { get; private set; } = 1.0f;

    /// <inheritdoc />
    public bool IsOnline { get; private set; } = true;

    /// <inheritdoc />
    public SystemCriticality Criticality => SystemCriticality.Important;

    /// <inheritdoc />
    public bool IsManuallyDisabled { get; private set; }

    /// <inheritdoc />
    public bool IsFailed { get; private set; }

    /// <summary>
    /// Gets remaining maneuvering fuel reserve.
    /// </summary>
    public float FuelReserve { get; private set; } = 100f;

    /// <summary>
    /// Gets a normalized deviation metric from desired course.
    /// Higher values indicate worse alignment.
    /// </summary>
    public float CourseError { get; private set; }

    /// <summary>
    /// Gets accumulated orbit stability bonus waiting to be consumed by the orchestrator.
    /// </summary>
    public float PendingOrbitStabilityBonus { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this module was powered during the latest tick.
    /// </summary>
    public bool IsPowered { get; private set; } = true;

    /// <summary>
    /// Gets current per-second orbit stability contribution.
    /// Positive values improve stability, negative values reduce stability.
    /// </summary>
    public float StabilityContributionPerSecond { get; private set; }

    /// <summary>
    /// Gets normalized navigation health in range 0-100.
    /// </summary>
    public float HealthPercent => Mathf.Clamp((FuelReserve + (100f - CourseError)) * 0.5f, 0f, 100f);

    /// <inheritdoc />
    public void Tick(float delta, bool isPowered)
    {
        IsPowered = isPowered && !IsManuallyDisabled;

        if (!IsOnline || !IsPowered)
        {
            CourseError = Mathf.Min(100f, CourseError + (0.7f * delta));
            StabilityContributionPerSecond = -0.08f;
            IsFailed = HealthPercent <= 0f;
            return;
        }

        float fuelBurn = (0.18f / Mathf.Max(0.2f, Efficiency)) * delta;
        FuelReserve = Mathf.Max(0f, FuelReserve - fuelBurn);

        float correctionStrength = 0.35f * Efficiency;
        CourseError = Mathf.Max(0f, CourseError - (correctionStrength * delta));

        if (FuelReserve <= 0f)
        {
            CourseError = Mathf.Min(100f, CourseError + (0.8f * delta));
        }

        StabilityContributionPerSecond = FuelReserve <= 0f ? -0.05f : Mathf.Lerp(-0.02f, 0.06f, 1f - (CourseError / 100f));
        IsFailed = HealthPercent <= 0f;
    }

    /// <summary>
    /// Changes online state for this module.
    /// </summary>
    /// <param name="online">True to enable burn and correction behavior.</param>
    public void SetOnline(bool online)
    {
        IsOnline = online;
        if (!online)
        {
            IsManuallyDisabled = false;
        }
    }

    /// <inheritdoc />
    public void SetManuallyDisabled(bool disabled)
    {
        IsManuallyDisabled = disabled;
    }

    /// <inheritdoc />
    public void ApplyUpgrade(UpgradeData upgrade)
    {
        Efficiency = Mathf.Max(0.2f, Efficiency + upgrade.EfficiencyBonus);
        PowerDraw = Mathf.Max(0f, PowerDraw + upgrade.PowerUsageDelta);
        PendingOrbitStabilityBonus += upgrade.OrbitStabilityBonus;
    }

    /// <summary>
    /// Returns and clears pending orbit stability bonus created by recent upgrades.
    /// </summary>
    /// <returns>Accumulated stability delta awaiting application.</returns>
    public float ConsumePendingOrbitStabilityBonus()
    {
        float value = PendingOrbitStabilityBonus;
        PendingOrbitStabilityBonus = 0f;
        return value;
    }

    /// <summary>
    /// Restores runtime state from persisted data.
    /// </summary>
    public void RestoreState(
        bool isOnline,
        bool isManuallyDisabled,
        bool isFailed,
        float powerDraw,
        float efficiency,
        float fuelReserve,
        float courseError,
        float pendingOrbitStabilityBonus,
        float stabilityContributionPerSecond)
    {
        IsOnline = isOnline;
        IsManuallyDisabled = isManuallyDisabled;
        IsFailed = isFailed;
        PowerDraw = Mathf.Max(0f, powerDraw);
        Efficiency = Mathf.Max(0.2f, efficiency);
        FuelReserve = Mathf.Clamp(fuelReserve, 0f, 100f);
        CourseError = Mathf.Clamp(courseError, 0f, 100f);
        PendingOrbitStabilityBonus = pendingOrbitStabilityBonus;
        StabilityContributionPerSecond = stabilityContributionPerSecond;
    }
}
