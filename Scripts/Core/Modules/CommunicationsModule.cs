using Godot;
using ZeroDayOrbit.Core.Interfaces;
using ZeroDayOrbit.Core.Models;

namespace ZeroDayOrbit.Core.Modules;

/// <summary>
/// Simulates communication link quality and baseline subsystem power usage.
/// </summary>
public sealed class CommunicationsModule : ISystemModule, IUpgradeable
{
    /// <inheritdoc />
    public string Name => "Communications";

    /// <inheritdoc />
    public float PowerDraw { get; private set; } = 1.4f;

    /// <inheritdoc />
    public float Efficiency { get; private set; } = 1.0f;

    /// <inheritdoc />
    public bool IsOnline { get; private set; } = true;

    /// <inheritdoc />
    public SystemCriticality Criticality => SystemCriticality.Optional;

    /// <inheritdoc />
    public bool IsManuallyDisabled { get; private set; }

    /// <inheritdoc />
    public bool IsFailed { get; private set; }

    /// <summary>
    /// Gets current communications link quality in percentage.
    /// </summary>
    public float SignalQuality { get; private set; } = 100f;

    /// <summary>
    /// Gets a value indicating whether this module was powered during the latest tick.
    /// </summary>
    public bool IsPowered { get; private set; } = true;

    /// <summary>
    /// Gets normalized communications health in range 0-100.
    /// </summary>
    public float HealthPercent => SignalQuality;

    /// <summary>
    /// Gets a value indicating whether communications are operational.
    /// </summary>
    public bool IsOperational => IsOnline && IsPowered && SignalQuality >= 25f;

    /// <inheritdoc />
    public void Tick(float delta, bool isPowered)
    {
        IsPowered = isPowered && !IsManuallyDisabled;

        if (!IsOnline || !IsPowered)
        {
            SignalQuality = Mathf.Max(0f, SignalQuality - (2.0f * delta));
            IsFailed = SignalQuality <= 0f;
            return;
        }

        float recoveryRate = 0.9f * Efficiency;
        SignalQuality = Mathf.Clamp(SignalQuality + (recoveryRate * delta), 0f, 100f);
        IsFailed = SignalQuality <= 0f;
    }

    /// <summary>
    /// Changes online state for this module.
    /// </summary>
    /// <param name="online">True to enable active signal recovery behavior.</param>
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
        SignalQuality = Mathf.Clamp(SignalQuality + (upgrade.EfficiencyBonus * 20f), 0f, 100f);
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
        float signalQuality)
    {
        IsOnline = isOnline;
        IsManuallyDisabled = isManuallyDisabled;
        IsFailed = isFailed;
        PowerDraw = Mathf.Max(0f, powerDraw);
        Efficiency = Mathf.Max(0.2f, efficiency);
        SignalQuality = Mathf.Clamp(signalQuality, 0f, 100f);
    }
}
