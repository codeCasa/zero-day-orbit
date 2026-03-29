using System.Collections.Generic;
using System.Linq;
using ZeroDayOrbit.Core.Interfaces;
using ZeroDayOrbit.Core.Models;
using ZeroDayOrbit.Core.Modules;
using ZeroDayOrbit.Core.Save;

namespace ZeroDayOrbit.Core.Managers;

/// <summary>
/// Central registry and update loop for all station system modules.
/// </summary>
public sealed class SystemManager
{
    private readonly List<ISystemModule> _modules = new();
    private float _demandPenaltyMultiplier = 1f;

    /// <summary>
    /// Gets latest station status snapshot generated during the most recent tick.
    /// </summary>
    public SystemStatusSnapshot LatestSnapshot { get; private set; } = new();

    /// <summary>
    /// Gets all registered modules as a read-only list.
    /// </summary>
    public IReadOnlyList<ISystemModule> Modules => _modules;

    /// <summary>
    /// Registers a module for simulation updates.
    /// </summary>
    /// <param name="module">Module instance to add if not already present.</param>
    public void RegisterModule(ISystemModule module)
    {
        if (_modules.Contains(module))
        {
            return;
        }

        _modules.Add(module);
    }

    /// <summary>
    /// Ticks all registered modules for the given time slice.
    /// Power is processed first, then module powered state is applied.
    /// </summary>
    /// <param name="delta">Elapsed simulation time in seconds.</param>
    /// <param name="isDaylight">Whether orbit is currently in daylight phase.</param>
    /// <returns>Snapshot of station status after updates.</returns>
    public SystemStatusSnapshot TickAll(float delta, bool isDaylight)
    {
        PowerSystemModule powerModule = GetModule<PowerSystemModule>();
        float baseConsumerDemand = _modules
            .Where(m => m is not PowerSystemModule && IsModuleActiveForConsumption(m))
            .Sum(m => m.PowerDraw);

        float consumerDemand = baseConsumerDemand * _demandPenaltyMultiplier;

        bool consumersPowered = true;

        if (powerModule != null)
        {
            powerModule.IsInDaylight = isDaylight;
            powerModule.ExternalLoad = consumerDemand;
            powerModule.Tick(delta, isPowered: true);
            consumersPowered = powerModule.AreConsumersPowered;
        }

        foreach (ISystemModule module in _modules)
        {
            if (module is PowerSystemModule)
            {
                continue;
            }

            bool isPowered = IsModuleActiveForConsumption(module) && consumersPowered;
            module.Tick(delta, isPowered);
        }

        LatestSnapshot = BuildSnapshot(powerModule, consumerDemand);
        return LatestSnapshot;
    }

    /// <summary>
    /// Computes the total power draw of all currently online modules.
    /// </summary>
    /// <returns>Summed online power draw.</returns>
    public float GetTotalPowerDraw()
    {
        float penalizedConsumerDraw = _modules
            .Where(m => m is not PowerSystemModule && IsModuleActiveForConsumption(m))
            .Sum(m => m.PowerDraw) * _demandPenaltyMultiplier;

        float powerSystemDraw = _modules
            .Where(m => m is PowerSystemModule && IsModuleActiveForConsumption(m))
            .Sum(m => m.PowerDraw);

        return powerSystemDraw + penalizedConsumerDraw;
    }

    /// <summary>
    /// Returns the first registered module matching the requested type.
    /// </summary>
    /// <typeparam name="T">Concrete module type to resolve.</typeparam>
    /// <returns>Module instance when found; otherwise null.</returns>
    public T GetModule<T>() where T : class, ISystemModule
    {
        return _modules.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Returns all registered modules matching the requested type.
    /// </summary>
    /// <typeparam name="T">Concrete module type to resolve.</typeparam>
    /// <returns>Read-only list snapshot of matching modules.</returns>
    public IReadOnlyList<T> GetModules<T>() where T : class, ISystemModule
    {
        return _modules.OfType<T>().ToList();
    }

    /// <summary>
    /// Finds a module by display name using case-insensitive match.
    /// </summary>
    /// <param name="moduleName">Module display name.</param>
    /// <returns>Matching module or null when not found.</returns>
    public ISystemModule FindModule(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return null;
        }

        return _modules.FirstOrDefault(m => string.Equals(m.Name, moduleName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sets manual disabled state for a module by name.
    /// </summary>
    /// <param name="moduleName">Module display name.</param>
    /// <param name="disabled">True to disable, false to enable.</param>
    /// <returns>True when module exists and state updated.</returns>
    public bool SetModuleDisabled(string moduleName, bool disabled)
    {
        ISystemModule module = FindModule(moduleName);
        if (module == null)
        {
            return false;
        }

        module.SetManuallyDisabled(disabled);
        return true;
    }

    /// <summary>
    /// Creates serializable module save entries.
    /// </summary>
    public List<ModuleSaveData> CreateModuleSaveData()
    {
        var result = new List<ModuleSaveData>(_modules.Count);

        foreach (ISystemModule module in _modules)
        {
            var entry = new ModuleSaveData
            {
                Name = module.Name,
                IsOnline = module.IsOnline,
                IsManuallyDisabled = module.IsManuallyDisabled,
                IsFailed = module.IsFailed,
                HealthPercent = module.HealthPercent,
                PowerDraw = module.PowerDraw,
                Efficiency = module.Efficiency,
                Criticality = module.Criticality.ToString()
            };

            switch (module)
            {
                case PowerSystemModule power:
                    entry.BatteryCapacity = power.BatteryCapacity;
                    entry.CurrentCharge = power.CurrentCharge;
                    entry.SolarGenerationRate = power.SolarGenerationRate;
                    entry.CurrentSolarGeneration = power.CurrentSolarGeneration;
                    entry.CurrentTotalDemand = power.CurrentTotalDemand;
                    entry.NetPowerBalance = power.NetPowerBalance;
                    entry.IsInPowerDeficit = power.IsInPowerDeficit;
                    break;
                case LifeSupportModule life:
                    entry.OxygenLevel = life.OxygenLevel;
                    entry.Co2Level = life.Co2Level;
                    entry.FailureTimer = life.FailureTimer;
                    break;
                case HeatModule heat:
                    entry.CurrentTemperature = heat.CurrentTemperature;
                    entry.UnsafeDuration = heat.UnsafeDuration;
                    entry.HeatDissipationBonus = heat.HeatDissipationRate;
                    break;
                case NavigationModule navigation:
                    entry.FuelReserve = navigation.FuelReserve;
                    entry.CourseError = navigation.CourseError;
                    entry.PendingOrbitStabilityBonus = navigation.PendingOrbitStabilityBonus;
                    entry.OrbitStabilityBonus = navigation.StabilityContributionPerSecond;
                    break;
                case CommunicationsModule comms:
                    entry.SignalQuality = comms.SignalQuality;
                    break;
            }

            result.Add(entry);
        }

        return result;
    }

    /// <summary>
    /// Restores module state from serialized save entries.
    /// </summary>
    public void LoadFromModuleSaveData(IReadOnlyList<ModuleSaveData> modules)
    {
        if (modules == null)
        {
            return;
        }

        foreach (ModuleSaveData saved in modules)
        {
            ISystemModule module = FindModule(saved.Name);
            if (module == null)
            {
                continue;
            }

            switch (module)
            {
                case PowerSystemModule power:
                    power.RestoreState(
                        saved.IsOnline,
                        saved.IsManuallyDisabled,
                        saved.IsFailed,
                        saved.PowerDraw,
                        saved.Efficiency,
                        saved.BatteryCapacity,
                        saved.CurrentCharge,
                        saved.SolarGenerationRate,
                        saved.CurrentSolarGeneration,
                        saved.CurrentTotalDemand,
                        saved.NetPowerBalance,
                        saved.IsInPowerDeficit);
                    break;
                case LifeSupportModule life:
                    life.RestoreState(
                        saved.IsOnline,
                        saved.IsManuallyDisabled,
                        saved.IsFailed,
                        saved.PowerDraw,
                        saved.Efficiency,
                        saved.OxygenLevel,
                        saved.Co2Level,
                        saved.FailureTimer);
                    break;
                case HeatModule heat:
                    heat.RestoreState(
                        saved.IsOnline,
                        saved.IsManuallyDisabled,
                        saved.IsFailed,
                        saved.PowerDraw,
                        saved.Efficiency,
                        saved.CurrentTemperature,
                        saved.UnsafeDuration,
                        saved.HeatDissipationBonus);
                    break;
                case NavigationModule navigation:
                    navigation.RestoreState(
                        saved.IsOnline,
                        saved.IsManuallyDisabled,
                        saved.IsFailed,
                        saved.PowerDraw,
                        saved.Efficiency,
                        saved.FuelReserve,
                        saved.CourseError,
                        saved.PendingOrbitStabilityBonus,
                        saved.OrbitStabilityBonus);
                    break;
                case CommunicationsModule comms:
                    comms.RestoreState(
                        saved.IsOnline,
                        saved.IsManuallyDisabled,
                        saved.IsFailed,
                        saved.PowerDraw,
                        saved.Efficiency,
                        saved.SignalQuality);
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the latest cached station snapshot.
    /// </summary>
    /// <returns>Last computed system snapshot.</returns>
    public SystemStatusSnapshot GetSnapshot()
    {
        return LatestSnapshot;
    }

    /// <summary>
    /// Sets a multiplier applied to non-power consumer demand.
    /// Useful for global penalties such as thermal inefficiency.
    /// </summary>
    /// <param name="multiplier">Demand multiplier; values less than 1 are clamped to 1.</param>
    public void SetDemandPenaltyMultiplier(float multiplier)
    {
        _demandPenaltyMultiplier = multiplier < 1f ? 1f : multiplier;
    }

    private SystemStatusSnapshot BuildSnapshot(PowerSystemModule powerModule, float consumerDemand)
    {
        var snapshot = new SystemStatusSnapshot
        {
            ConsumerPowerDemand = consumerDemand,
            TotalPowerDraw = GetTotalPowerDraw(),
            SolarGeneration = powerModule?.CurrentSolarGeneration ?? 0f,
            NetPowerBalance = powerModule?.NetPowerBalance ?? 0f,
            BatteryPercent = powerModule?.BatteryPercent ?? 0f,
            IsPowerDeficit = powerModule?.IsInPowerDeficit ?? false
        };

        foreach (ISystemModule module in _modules)
        {
            snapshot.Modules.Add(new ModuleStatus
            {
                Name = module.Name,
                IsOnline = module.IsOnline,
                IsPowered = ResolvePoweredState(module),
                Efficiency = module.Efficiency,
                PowerDraw = module.PowerDraw,
                HealthPercent = module.HealthPercent,
                Criticality = module.Criticality,
                IsFailed = module.IsFailed,
                IsManuallyDisabled = module.IsManuallyDisabled,
                Detail = BuildDetail(module)
            });
        }

        return snapshot;
    }

    private static bool ResolvePoweredState(ISystemModule module)
    {
        return module switch
        {
            PowerSystemModule power => power.IsOnline && !power.IsManuallyDisabled && !power.IsFailed,
            LifeSupportModule lifeSupport => lifeSupport.IsPowered,
            HeatModule heat => heat.IsPowered,
            CommunicationsModule comms => comms.IsPowered,
            NavigationModule navigation => navigation.IsPowered,
            _ => module.IsOnline && !module.IsManuallyDisabled && !module.IsFailed
        };
    }

    private static string BuildDetail(ISystemModule module)
    {
        return module switch
        {
            PowerSystemModule power => $"Battery {power.CurrentCharge:F1}/{power.BatteryCapacity:F1}",
            LifeSupportModule lifeSupport => $"O2 {lifeSupport.OxygenLevel:F1}% / CO2 {lifeSupport.Co2Level:F1}%",
            HeatModule heat => $"Temp {heat.CurrentTemperature:F1}C",
            CommunicationsModule comms => $"Signal {comms.SignalQuality:F1}%",
            NavigationModule navigation => $"Fuel {navigation.FuelReserve:F1}% / Error {navigation.CourseError:F1}%",
            _ => string.Empty
        };
    }

    private static bool IsModuleActiveForConsumption(ISystemModule module)
    {
        return module.IsOnline && !module.IsManuallyDisabled && !module.IsFailed;
    }
}
