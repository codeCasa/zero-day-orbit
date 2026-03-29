using Godot;
using ZeroDayOrbit.Core.Interfaces;
using ZeroDayOrbit.Core.Models;

namespace ZeroDayOrbit.Core.Modules;

/// <summary>
/// Simulates station power generation, consumption, and battery state.
/// </summary>
public sealed class PowerSystemModule : ISystemModule, IUpgradeable
{
    /// <inheritdoc />
    public string Name => "Power";

    /// <inheritdoc />
    public float PowerDraw { get; private set; } = 0.75f;

    /// <inheritdoc />
    public float Efficiency { get; private set; } = 1.0f;

    /// <inheritdoc />
    public bool IsOnline { get; private set; } = true;

    /// <inheritdoc />
    public float HealthPercent => BatteryPercent;

    /// <inheritdoc />
    public SystemCriticality Criticality => SystemCriticality.Critical;

    /// <inheritdoc />
    public bool IsManuallyDisabled { get; private set; }

    /// <inheritdoc />
    public bool IsFailed { get; private set; }

    /// <summary>
    /// Gets the maximum battery charge capacity.
    /// </summary>
    public float BatteryCapacity { get; private set; } = 100f;

    /// <summary>
    /// Gets the current battery charge level.
    /// </summary>
    public float CurrentCharge { get; private set; } = 75f;

    /// <summary>
    /// Gets the baseline solar generation rate in daylight.
    /// </summary>
    public float SolarGenerationRate { get; private set; } = 6f;

    /// <summary>
    /// Gets current generated solar power units for the frame.
    /// </summary>
    public float CurrentSolarGeneration { get; private set; }

    /// <summary>
    /// Gets current generated power for this frame.
    /// </summary>
    public float CurrentGeneration => CurrentSolarGeneration;

    /// <summary>
    /// Gets current total station demand including this module draw.
    /// </summary>
    public float CurrentTotalDemand { get; private set; }

    /// <summary>
    /// Gets current total station consumption for this frame.
    /// </summary>
    public float CurrentConsumption => CurrentTotalDemand;

    /// <summary>
    /// Gets current net power balance. Positive values charge the battery.
    /// </summary>
    public float NetPowerBalance { get; private set; }

    /// <summary>
    /// Gets net power value for this frame.
    /// </summary>
    public float NetPower => NetPowerBalance;

    /// <summary>
    /// Gets battery charge percentage in range 0-100.
    /// </summary>
    public float BatteryPercent => BatteryCapacity <= 0f ? 0f : (CurrentCharge / BatteryCapacity) * 100f;

    /// <summary>
    /// Gets a value indicating whether station demand cannot be satisfied.
    /// </summary>
    public bool IsInPowerDeficit { get; private set; }

    /// <summary>
    /// Gets a value indicating whether non-power systems can run this frame.
    /// </summary>
    public bool AreConsumersPowered { get; private set; } = true;

    /// <summary>
    /// Gets or sets aggregate external load from other online modules.
    /// </summary>
    public float ExternalLoad { get; set; }

    /// <summary>
    /// Gets or sets whether the station is currently in direct sunlight.
    /// </summary>
    public bool IsInDaylight { get; set; } = true;

    /// <inheritdoc />
    public void Tick(float delta, bool isPowered)
    {
        if (!IsOnline || IsManuallyDisabled)
        {
            CurrentSolarGeneration = 0f;
            CurrentTotalDemand = IsManuallyDisabled ? ExternalLoad : 0f;
            NetPowerBalance = 0f;
            IsInPowerDeficit = true;
            AreConsumersPowered = false;
            IsFailed = !IsManuallyDisabled;
            return;
        }

        float generation = IsInDaylight ? SolarGenerationRate * Efficiency : 0f;
        float consumption = ExternalLoad + PowerDraw;
        UpdatePower(generation, consumption, delta);
        IsFailed = CurrentCharge <= 0f && IsInPowerDeficit;
    }

    /// <summary>
    /// Applies generation/consumption to battery state for a simulation step.
    /// </summary>
    /// <param name="generation">Generated power for this frame.</param>
    /// <param name="consumption">Consumed power for this frame.</param>
    /// <param name="delta">Elapsed simulation time in seconds.</param>
    public void UpdatePower(float generation, float consumption, float delta)
    {
        CurrentSolarGeneration = Mathf.Max(0f, generation);
        CurrentTotalDemand = Mathf.Max(0f, consumption);
        NetPowerBalance = CurrentSolarGeneration - CurrentTotalDemand;

        if (NetPowerBalance >= 0f)
        {
            CurrentCharge = Mathf.Clamp(CurrentCharge + (NetPowerBalance * delta), 0f, BatteryCapacity);
            IsInPowerDeficit = false;
            AreConsumersPowered = true;
            return;
        }

        float requiredBattery = -NetPowerBalance * delta;
        if (CurrentCharge >= requiredBattery)
        {
            CurrentCharge = Mathf.Clamp(CurrentCharge - requiredBattery, 0f, BatteryCapacity);
            IsInPowerDeficit = false;
            AreConsumersPowered = true;
            return;
        }

        CurrentCharge = 0f;
        IsInPowerDeficit = true;
        AreConsumersPowered = false;
    }

    /// <summary>
    /// Changes online state for this module.
    /// </summary>
    /// <param name="online">True to enable simulation updates; false to pause generation/consumption.</param>
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
        BatteryCapacity = Mathf.Max(10f, BatteryCapacity + upgrade.BatteryCapacityDelta);
        CurrentCharge = Mathf.Min(CurrentCharge, BatteryCapacity);
        Efficiency = Mathf.Max(0.2f, Efficiency + upgrade.EfficiencyBonus);
        PowerDraw = Mathf.Max(0f, PowerDraw + upgrade.PowerUsageDelta);

        if (upgrade.BatteryCapacityDelta > 0f)
        {
            CurrentCharge = Mathf.Min(CurrentCharge + (upgrade.BatteryCapacityDelta * 0.5f), BatteryCapacity);
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
        float batteryCapacity,
        float currentCharge,
        float solarGenerationRate,
        float currentSolarGeneration,
        float currentTotalDemand,
        float netPowerBalance,
        bool isInPowerDeficit)
    {
        IsOnline = isOnline;
        IsManuallyDisabled = isManuallyDisabled;
        IsFailed = isFailed;
        PowerDraw = Mathf.Max(0f, powerDraw);
        Efficiency = Mathf.Max(0.2f, efficiency);
        BatteryCapacity = Mathf.Max(1f, batteryCapacity);
        CurrentCharge = Mathf.Clamp(currentCharge, 0f, BatteryCapacity);
        SolarGenerationRate = Mathf.Max(0f, solarGenerationRate);
        CurrentSolarGeneration = Mathf.Max(0f, currentSolarGeneration);
        CurrentTotalDemand = Mathf.Max(0f, currentTotalDemand);
        NetPowerBalance = netPowerBalance;
        IsInPowerDeficit = isInPowerDeficit;
        AreConsumersPowered = !isInPowerDeficit || CurrentCharge > 0f;
    }
}
