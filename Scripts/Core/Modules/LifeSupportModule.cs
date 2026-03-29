using Godot;
using ZeroDayOrbit.Core.Interfaces;
using ZeroDayOrbit.Core.Models;

namespace ZeroDayOrbit.Core.Modules;

/// <summary>
/// Simulates atmospheric maintenance, including oxygen consumption and CO2 accumulation.
/// </summary>
public sealed class LifeSupportModule : ISystemModule, IUpgradeable
{
    /// <inheritdoc />
    public string Name => "Life Support";

    /// <inheritdoc />
    public float PowerDraw { get; private set; } = 2.8f;

    /// <inheritdoc />
    public float Efficiency { get; private set; } = 1.0f;

    /// <inheritdoc />
    public bool IsOnline { get; private set; } = true;

    /// <inheritdoc />
    public SystemCriticality Criticality => SystemCriticality.Critical;

    /// <inheritdoc />
    public bool IsManuallyDisabled { get; private set; }

    /// <inheritdoc />
    public bool IsFailed { get; private set; }

    /// <summary>
    /// Gets the current oxygen percentage.
    /// </summary>
    public float OxygenLevel { get; private set; } = 100f;

    /// <summary>
    /// Gets the current CO2 percentage.
    /// </summary>
    public float Co2Level { get; private set; } = 0f;

    /// <summary>
    /// Gets a timer that increases while environmental conditions are unsafe.
    /// </summary>
    public float FailureTimer { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this module was powered during the latest tick.
    /// </summary>
    public bool IsPowered { get; private set; } = true;

    /// <summary>
    /// Gets normalized life support health percentage in range 0-100.
    /// </summary>
    public float HealthPercent => Mathf.Clamp((OxygenLevel + (100f - Co2Level)) * 0.5f, 0f, 100f);

    /// <summary>
    /// Gets a value indicating whether atmospheric conditions are in a dangerous range.
    /// </summary>
    public bool IsCritical => OxygenLevel < 20f || Co2Level > 80f;

    /// <inheritdoc />
    public void Tick(float delta, bool isPowered)
    {
        IsPowered = isPowered && !IsManuallyDisabled;

        if (!IsOnline || !IsPowered)
        {
            FailureTimer += delta;
            OxygenLevel = Mathf.Max(0f, OxygenLevel - 1.5f * delta);
            Co2Level = Mathf.Min(100f, Co2Level + 1.6f * delta);
            IsFailed = OxygenLevel <= 0f || Co2Level >= 100f || HealthPercent <= 0f;
            return;
        }

        float restoration = 0.6f * Efficiency * delta;
        OxygenLevel = Mathf.Clamp(OxygenLevel + restoration, 0f, 100f);
        Co2Level = Mathf.Clamp(Co2Level - restoration, 0f, 100f);

        if (IsCritical)
        {
            FailureTimer += delta;
        }
        else
        {
            FailureTimer = Mathf.Max(0f, FailureTimer - (0.75f * delta));
        }

        IsFailed = OxygenLevel <= 0f || Co2Level >= 100f || HealthPercent <= 0f;
    }

    /// <summary>
    /// Changes online state for this module.
    /// </summary>
    /// <param name="online">True to run active atmospheric processing logic.</param>
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

        if (upgrade.EfficiencyBonus > 0f)
        {
            OxygenLevel = Mathf.Min(100f, OxygenLevel + 1.5f);
            Co2Level = Mathf.Max(0f, Co2Level - 1.5f);
        }
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
        float oxygenLevel,
        float co2Level,
        float failureTimer)
    {
        IsOnline = isOnline;
        IsManuallyDisabled = isManuallyDisabled;
        IsFailed = isFailed;
        PowerDraw = Mathf.Max(0f, powerDraw);
        Efficiency = Mathf.Max(0.2f, efficiency);
        OxygenLevel = Mathf.Clamp(oxygenLevel, 0f, 100f);
        Co2Level = Mathf.Clamp(co2Level, 0f, 100f);
        FailureTimer = Mathf.Max(0f, failureTimer);
    }
}
