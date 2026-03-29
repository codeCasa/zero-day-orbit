using Godot;
using ZeroDayOrbit.Core.Interfaces;
using ZeroDayOrbit.Core.Models;

namespace ZeroDayOrbit.Core.Modules;

/// <summary>
/// Simulates thermal balance and safe operating temperature bounds.
/// </summary>
public sealed class HeatModule : ISystemModule, IUpgradeable
{
    /// <inheritdoc />
    public string Name => "Heat";

    /// <inheritdoc />
    public float PowerDraw { get; private set; } = 1.9f;

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
    /// Gets the current internal station temperature in degrees Celsius.
    /// </summary>
    public float CurrentTemperature { get; private set; } = 22f;

    /// <summary>
    /// Gets the lower bound of the acceptable thermal range.
    /// </summary>
    public float SafeMinimumTemperature { get; private set; } = 16f;

    /// <summary>
    /// Gets the upper bound of the acceptable thermal range.
    /// </summary>
    public float SafeMaximumTemperature { get; private set; } = 30f;

    /// <summary>
    /// Gets thermal dissipation capacity applied each tick while online.
    /// </summary>
    public float HeatDissipationRate { get; private set; } = 1.1f;

    /// <summary>
    /// Gets a value indicating whether this module was powered during the latest tick.
    /// </summary>
    public bool IsPowered { get; private set; } = true;

    /// <summary>
    /// Gets total time spent outside safe temperature bounds.
    /// </summary>
    public float UnsafeDuration { get; private set; }

    /// <summary>
    /// Gets normalized thermal health percentage in range 0-100.
    /// </summary>
    public float HealthPercent
    {
        get
        {
            float target = 22f;
            float maxDeviation = 35f;
            float deviation = Mathf.Abs(CurrentTemperature - target);
            return Mathf.Clamp(100f - ((deviation / maxDeviation) * 100f), 0f, 100f);
        }
    }

    /// <summary>
    /// Gets a value indicating whether current temperature is within safe bounds.
    /// </summary>
    public bool IsInSafeRange => CurrentTemperature >= SafeMinimumTemperature && CurrentTemperature <= SafeMaximumTemperature;

    /// <inheritdoc />
    public void Tick(float delta, bool isPowered)
    {
        IsPowered = isPowered && !IsManuallyDisabled;
        float targetTemperature = 22f;

        if (IsOnline && isPowered)
        {
            CurrentTemperature = Mathf.Lerp(CurrentTemperature, targetTemperature, Mathf.Clamp(0.65f * Efficiency * delta, 0f, 1f));
        }
        else
        {
            CurrentTemperature += 1.15f * delta;
        }

        CurrentTemperature = Mathf.Clamp(CurrentTemperature, -40f, 120f);

        if (IsInSafeRange)
        {
            UnsafeDuration = Mathf.Max(0f, UnsafeDuration - (0.5f * delta));
        }
        else
        {
            UnsafeDuration += delta;
        }

        IsFailed = HealthPercent <= 0f;
    }

    /// <summary>
    /// Changes online state for this module.
    /// </summary>
    /// <param name="online">True to enable active cooling behavior.</param>
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
        HeatDissipationRate = Mathf.Max(0.05f, HeatDissipationRate + upgrade.HeatDissipationBonus);
        Efficiency = Mathf.Max(0.2f, Efficiency + upgrade.EfficiencyBonus);
        PowerDraw = Mathf.Max(0f, PowerDraw + upgrade.PowerUsageDelta);
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
        float currentTemperature,
        float unsafeDuration,
        float heatDissipationRate)
    {
        IsOnline = isOnline;
        IsManuallyDisabled = isManuallyDisabled;
        IsFailed = isFailed;
        PowerDraw = Mathf.Max(0f, powerDraw);
        Efficiency = Mathf.Max(0.2f, efficiency);
        CurrentTemperature = Mathf.Clamp(currentTemperature, -40f, 120f);
        UnsafeDuration = Mathf.Max(0f, unsafeDuration);
        HeatDissipationRate = Mathf.Max(0.05f, heatDissipationRate);
    }
}
