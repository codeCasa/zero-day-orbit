using System.Collections.Generic;
using System.Linq;
using Godot;
using ZeroDayOrbit.Core.Interfaces;
using ZeroDayOrbit.Core.Loaders;
using ZeroDayOrbit.Core.Models;
using ZeroDayOrbit.Core.Modules;
using ZeroDayOrbit.Core.Save;

namespace ZeroDayOrbit.Core.Managers;

/// <summary>
/// Manages available and applied upgrades and routes effects to target modules.
/// </summary>
public sealed class UpgradeManager
{
    private readonly List<UpgradeData> _allUpgrades = new();
    private readonly List<UpgradeData> _appliedUpgrades = new();
    private readonly Dictionary<string, int> _applicationCounts = new();

    /// <summary>
    /// Gets cumulative station weight contributed by applied upgrades.
    /// </summary>
    public float StationWeight { get; private set; }

    /// <summary>
    /// Gets all loaded upgrade definitions.
    /// </summary>
    public IReadOnlyList<UpgradeData> AllUpgrades => _allUpgrades;

    /// <summary>
    /// Gets all upgrades already applied to the station.
    /// </summary>
    public IReadOnlyList<UpgradeData> AppliedUpgrades => _appliedUpgrades;

    /// <summary>
    /// Gets per-upgrade application counts.
    /// </summary>
    public IReadOnlyDictionary<string, int> ApplicationCounts => _applicationCounts;

    /// <summary>
    /// Clears runtime upgrade state and loaded definitions.
    /// </summary>
    public void Reset()
    {
        _allUpgrades.Clear();
        _appliedUpgrades.Clear();
        _applicationCounts.Clear();
        StationWeight = 0f;
    }

    /// <summary>
    /// Loads upgrade definitions from a JSON config file.
    /// </summary>
    /// <param name="resourcePath">Path to JSON config under res://.</param>
    /// <returns>Number of valid loaded definitions.</returns>
    public int LoadFromJson(string resourcePath)
    {
        Reset();

        if (!UpgradeConfigLoader.TryLoad(resourcePath, out List<UpgradeData> loaded, out string error))
        {
            GD.PushWarning($"[UpgradeManager] {error}");
            return 0;
        }

        foreach (UpgradeData upgrade in loaded)
        {
            if (!Validate(upgrade, out string validationError))
            {
                GD.PushWarning($"[UpgradeManager] Skipping invalid upgrade '{upgrade?.Id}': {validationError}");
                continue;
            }

            if (_allUpgrades.Any(u => u.Id == upgrade.Id))
            {
                GD.PushWarning($"[UpgradeManager] Duplicate upgrade id '{upgrade.Id}' ignored.");
                continue;
            }

            _allUpgrades.Add(upgrade);
        }

        GD.Print($"[UpgradeManager] Loaded {_allUpgrades.Count} upgrades from {resourcePath}");
        return _allUpgrades.Count;
    }

    /// <summary>
    /// Adds an upgrade to the loaded upgrade set.
    /// </summary>
    /// <param name="upgrade">Upgrade definition to expose for selection.</param>
    public void AddUpgradeDefinition(UpgradeData upgrade)
    {
        if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.Id))
        {
            return;
        }

        if (_allUpgrades.Any(u => u.Id == upgrade.Id))
        {
            return;
        }

        _allUpgrades.Add(upgrade);
    }

    /// <summary>
    /// Gets upgrades that are currently enabled and can still be applied by rule checks.
    /// </summary>
    /// <param name="systemManager">System manager containing candidate target modules.</param>
    /// <returns>Applicable upgrade definitions.</returns>
    public IReadOnlyList<UpgradeData> GetApplicableUpgrades(SystemManager systemManager)
    {
        return _allUpgrades.Where(u => CanApplyUpgrade(u.Id, systemManager, out _)).ToList();
    }

    /// <summary>
    /// Gets upgrades that are currently enabled, regardless of prerequisites or cap rules.
    /// </summary>
    /// <returns>Enabled upgrades.</returns>
    public IReadOnlyList<UpgradeData> GetEnabledUpgrades()
    {
        return _allUpgrades.Where(u => u.IsEnabled).ToList();
    }

    /// <summary>
    /// Determines whether an upgrade can currently be applied.
    /// </summary>
    /// <param name="upgradeId">Upgrade identifier.</param>
    /// <param name="systemManager">System manager containing candidate targets.</param>
    /// <param name="reason">Reason text when the upgrade cannot be applied.</param>
    /// <returns>True when the upgrade can be applied now.</returns>
    public bool CanApplyUpgrade(string upgradeId, SystemManager systemManager, out string reason)
    {
        reason = string.Empty;
        UpgradeData upgrade = _allUpgrades.FirstOrDefault(u => u.Id == upgradeId);
        if (upgrade == null)
        {
            reason = $"Upgrade '{upgradeId}' does not exist.";
            return false;
        }

        if (!upgrade.IsEnabled)
        {
            reason = $"Upgrade '{upgrade.Name}' is disabled.";
            return false;
        }

        int appliedCount = GetApplicationCount(upgrade.Id);
        if (!upgrade.IsRepeatable && appliedCount >= 1)
        {
            reason = $"Upgrade '{upgrade.Name}' is already applied.";
            return false;
        }

        if (upgrade.IsRepeatable && upgrade.MaxApplications > 0 && appliedCount >= upgrade.MaxApplications)
        {
            reason = $"Upgrade '{upgrade.Name}' reached max applications ({upgrade.MaxApplications}).";
            return false;
        }

        foreach (string prerequisiteId in upgrade.PrerequisiteUpgradeIds)
        {
            if (string.IsNullOrWhiteSpace(prerequisiteId))
            {
                continue;
            }

            if (GetApplicationCount(prerequisiteId) <= 0)
            {
                reason = $"Missing prerequisite '{prerequisiteId}' for '{upgrade.Name}'.";
                return false;
            }
        }

        bool hasTarget = systemManager.Modules.Any(module => module is IUpgradeable && MatchesTarget(upgrade.TargetType, module));
        if (!hasTarget)
        {
            reason = $"No matching target module for '{upgrade.Name}'.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies an upgrade to matching module targets and records resulting metadata.
    /// </summary>
    /// <param name="upgradeId">Upgrade identifier to apply.</param>
    /// <param name="systemManager">System manager containing target modules.</param>
    /// <param name="orbitManager">Optional orbit manager for stability side effects.</param>
    /// <param name="message">Result message for debug display.</param>
    /// <returns>True when at least one module accepted the upgrade; otherwise false.</returns>
    public bool ApplyUpgrade(string upgradeId, SystemManager systemManager, OrbitManager orbitManager, out string message)
    {
        message = string.Empty;

        if (!CanApplyUpgrade(upgradeId, systemManager, out string reason))
        {
            message = reason;
            return false;
        }

        UpgradeData upgrade = _allUpgrades.First(u => u.Id == upgradeId);
        bool appliedToAnyModule = false;

        foreach (ISystemModule module in systemManager.Modules)
        {
            if (module is not IUpgradeable upgradeable)
            {
                continue;
            }

            if (!MatchesTarget(upgrade.TargetType, module))
            {
                continue;
            }

            upgradeable.ApplyUpgrade(upgrade);
            appliedToAnyModule = true;
        }

        if (!appliedToAnyModule)
        {
            message = $"No module accepted '{upgrade.Name}'.";
            return false;
        }

        _appliedUpgrades.Add(upgrade);
        _applicationCounts[upgrade.Id] = GetApplicationCount(upgrade.Id) + 1;
        StationWeight += upgrade.Weight;

        if (orbitManager != null && upgrade.OrbitStabilityBonus != 0f)
        {
            orbitManager.ModifyOrbitStability(upgrade.OrbitStabilityBonus);
        }

        message = $"Applied upgrade: {upgrade.Name}";

        return true;
    }

    /// <summary>
    /// Gets number of times an upgrade has been applied.
    /// </summary>
    /// <param name="upgradeId">Upgrade identifier.</param>
    /// <returns>Application count for the upgrade.</returns>
    public int GetApplicationCount(string upgradeId)
    {
        return _applicationCounts.TryGetValue(upgradeId, out int count) ? count : 0;
    }

    /// <summary>
    /// Creates serializable upgrade progression data.
    /// </summary>
    public UpgradeSaveData CreateSaveData()
    {
        return new UpgradeSaveData
        {
            AppliedUpgradeIds = _appliedUpgrades.Select(u => u.Id).ToList(),
            ApplicationCounts = new Dictionary<string, int>(_applicationCounts),
            StationWeight = StationWeight
        };
    }

    /// <summary>
    /// Restores upgrade progression data after definitions are loaded.
    /// </summary>
    public void LoadFromSaveData(UpgradeSaveData data)
    {
        _appliedUpgrades.Clear();
        _applicationCounts.Clear();

        if (data == null)
        {
            StationWeight = 0f;
            return;
        }

        foreach ((string id, int count) in data.ApplicationCounts)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            _applicationCounts[id] = count < 0 ? 0 : count;
        }

        foreach (string id in data.AppliedUpgradeIds)
        {
            UpgradeData match = _allUpgrades.FirstOrDefault(u => u.Id == id);
            if (match != null)
            {
                _appliedUpgrades.Add(match);
            }
        }

        StationWeight = data.StationWeight;
    }

    /// <summary>
    /// Determines whether a module matches the target category of an upgrade.
    /// </summary>
    /// <param name="targetType">Declared upgrade target type.</param>
    /// <param name="module">Module candidate to test.</param>
    /// <returns>True when the module should receive the upgrade.</returns>
    private static bool MatchesTarget(UpgradeTargetType targetType, ISystemModule module)
    {
        return targetType switch
        {
            UpgradeTargetType.PowerSystem => module is PowerSystemModule,
            UpgradeTargetType.LifeSupport => module is LifeSupportModule,
            UpgradeTargetType.Heat => module is HeatModule,
            UpgradeTargetType.Navigation => module is NavigationModule,
            UpgradeTargetType.Communications => module is CommunicationsModule,
            UpgradeTargetType.StationGlobal => true,
            _ => false
        };
    }

    private static bool Validate(UpgradeData upgrade, out string reason)
    {
        reason = string.Empty;

        if (upgrade == null)
        {
            reason = "Upgrade entry is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(upgrade.Id))
        {
            reason = "Missing Id.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(upgrade.Name))
        {
            reason = "Missing Name.";
            return false;
        }

        if (upgrade.Cost < 0)
        {
            reason = "Cost cannot be negative.";
            return false;
        }

        if (!upgrade.IsRepeatable && upgrade.MaxApplications <= 0)
        {
            upgrade.MaxApplications = 1;
        }

        upgrade.PrerequisiteUpgradeIds ??= [];
        upgrade.Category ??= "General";
        upgrade.IconKey ??= string.Empty;
        return true;
    }
}
