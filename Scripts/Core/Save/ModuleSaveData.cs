namespace ZeroDayOrbit.Core.Save;

/// <summary>
/// Serializable system module state payload.
/// </summary>
public sealed class ModuleSaveData
{
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsManuallyDisabled { get; set; }
    public bool IsFailed { get; set; }
    public float HealthPercent { get; set; }
    public float PowerDraw { get; set; }
    public float Efficiency { get; set; }
    public string Criticality { get; set; } = string.Empty;

    // Power module specific
    public float BatteryCapacity { get; set; }
    public float CurrentCharge { get; set; }
    public float SolarGenerationRate { get; set; }
    public float CurrentSolarGeneration { get; set; }
    public float CurrentTotalDemand { get; set; }
    public float NetPowerBalance { get; set; }
    public bool IsInPowerDeficit { get; set; }

    // Life support specific
    public float OxygenLevel { get; set; }
    public float Co2Level { get; set; }
    public float FailureTimer { get; set; }

    // Heat specific
    public float CurrentTemperature { get; set; }
    public float UnsafeDuration { get; set; }
    public float HeatDissipationBonus { get; set; }

    // Navigation specific
    public float FuelReserve { get; set; }
    public float CourseError { get; set; }
    public float PendingOrbitStabilityBonus { get; set; }
    public float OrbitStabilityBonus { get; set; }

    // Communications specific
    public float SignalQuality { get; set; }
}
