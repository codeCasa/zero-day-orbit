using System.Linq;
using System.Collections.Generic;
using ZeroDayOrbit.Core.Managers;
using ZeroDayOrbit.Core.Models;
using ZeroDayOrbit.Core.Modules;
using ZeroDayOrbit.Core.Save;

namespace ZeroDayOrbit.Core.Controllers;

/// <summary>
/// Coordinates high-level gameplay simulation by updating managers in the correct order.
/// </summary>
public sealed class GameController
{
    private const float PowerFailureGraceSeconds = 8f;
    private const float HeatFailureDemandPenalty = 1.25f;
    private const float HeatFailureOrbitPenaltyPerSecond = 0.05f;
    private const float NavigationFailureOrbitPenaltyPerSecond = 0.20f;
    private const float CommunicationsFailureOrbitPenaltyPerSecond = 0.01f;
    private const string DefaultUpgradeConfigPath = "res://Data/upgrades.json";

    private float _powerFailureTimer;
    private float _pendingDemandPenaltyMultiplier = 1f;
    private readonly string _upgradeConfigPath;
    private int _selectedUpgradeIndex;
    private float _sessionElapsedSeconds;

    /// <summary>
    /// Gets the system manager containing all active station modules.
    /// </summary>
    public SystemManager SystemManager { get; }

    /// <summary>
    /// Gets the orbit manager for day/night and stability state.
    /// </summary>
    public OrbitManager OrbitManager { get; }

    /// <summary>
    /// Gets the upgrade manager for available/applied upgrade flow.
    /// </summary>
    public UpgradeManager UpgradeManager { get; }

    /// <summary>
    /// Gets the cosmic event manager for random event orchestration.
    /// </summary>
    public CosmicEventManager CosmicEventManager { get; }

    /// <summary>
    /// Gets a value indicating whether simulation updates should run.
    /// </summary>
    public bool IsGameRunning { get; private set; }

    /// <summary>
    /// Gets current high-level game state.
    /// </summary>
    public GameState State { get; private set; } = GameState.Running;

    /// <summary>
    /// Gets the reason for game over, if one has occurred.
    /// </summary>
    public GameOverReason GameOverReason { get; private set; } = GameOverReason.None;

    /// <summary>
    /// Gets human-readable game over message.
    /// </summary>
    public string GameOverMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Gets latest system status snapshot for debug and future HUD bindings.
    /// </summary>
    public SystemStatusSnapshot CurrentSystemStatus { get; private set; } = new();

    /// <summary>
    /// Gets latest orbit status snapshot for debug and future HUD bindings.
    /// </summary>
    public OrbitStatusSnapshot CurrentOrbitStatus { get; private set; } = new();

    /// <summary>
    /// Gets latest interaction result message for debug/UI feedback.
    /// </summary>
    public string LastActionMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new controller and allows optional dependency injection for testing.
    /// </summary>
    /// <param name="systemManager">System manager instance or null to create default.</param>
    /// <param name="orbitManager">Orbit manager instance or null to create default.</param>
    /// <param name="upgradeManager">Upgrade manager instance or null to create default.</param>
    /// <param name="cosmicEventManager">Cosmic event manager instance or null to create default.</param>
    /// <param name="upgradeConfigPath">Upgrade config resource path.</param>
    public GameController(
        SystemManager systemManager = null,
        OrbitManager orbitManager = null,
        UpgradeManager upgradeManager = null,
        CosmicEventManager cosmicEventManager = null,
        string upgradeConfigPath = DefaultUpgradeConfigPath)
    {
        SystemManager = systemManager ?? new SystemManager();
        OrbitManager = orbitManager ?? new OrbitManager();
        UpgradeManager = upgradeManager ?? new UpgradeManager();
        CosmicEventManager = cosmicEventManager ?? new CosmicEventManager();
        _upgradeConfigPath = string.IsNullOrWhiteSpace(upgradeConfigPath) ? DefaultUpgradeConfigPath : upgradeConfigPath;
    }

    /// <summary>
    /// Initializes baseline modules and starter upgrade definitions.
    /// </summary>
    public void StartGame()
    {
        RegisterDefaultModulesIfNeeded();
        int loadedUpgrades = UpgradeManager.LoadFromJson(_upgradeConfigPath);
        LastActionMessage = loadedUpgrades > 0
            ? $"Loaded {loadedUpgrades} upgrades from config."
            : "No upgrades loaded from config.";

        _pendingDemandPenaltyMultiplier = 1f;
        _sessionElapsedSeconds = 0f;
        _selectedUpgradeIndex = 0;
        SystemManager.SetDemandPenaltyMultiplier(_pendingDemandPenaltyMultiplier);
        CurrentOrbitStatus = OrbitManager.CreateSnapshot();
        CurrentSystemStatus = SystemManager.TickAll(0f, OrbitManager.IsDaytime);
        IsGameRunning = true;
        State = GameState.Running;
        GameOverReason = GameOverReason.None;
        GameOverMessage = string.Empty;
        _powerFailureTimer = 0f;
    }

    /// <summary>
    /// Advances one frame of gameplay simulation.
    /// </summary>
    /// <param name="delta">Elapsed simulation time in seconds.</param>
    public void UpdateGame(float delta)
    {
        if (!IsGameRunning)
        {
            return;
        }

        OrbitManager.Update(delta);
        _sessionElapsedSeconds += delta;
        SystemManager.SetDemandPenaltyMultiplier(_pendingDemandPenaltyMultiplier);
        CurrentSystemStatus = SystemManager.TickAll(delta, OrbitManager.IsDaytime);
        CurrentOrbitStatus = OrbitManager.CreateSnapshot();

        NavigationModule navigation = SystemManager.GetModule<NavigationModule>();
        if (navigation != null)
        {
            float pendingStability = navigation.ConsumePendingOrbitStabilityBonus();
            if (pendingStability != 0f)
            {
                OrbitManager.ModifyOrbitStability(pendingStability);
            }

            OrbitManager.ModifyOrbitStability(navigation.StabilityContributionPerSecond * delta);
        }

        ApplyFailureConsequences(delta);

        EvaluateLoseConditions(delta);
        CurrentSystemStatus = SystemManager.GetSnapshot();
        CurrentOrbitStatus = OrbitManager.CreateSnapshot();
    }

    /// <summary>
    /// Registers the default Phase 1 station module set.
    /// </summary>
    private void RegisterDefaultModulesIfNeeded()
    {
        if (SystemManager.Modules.Count > 0)
        {
            return;
        }

        SystemManager.RegisterModule(new PowerSystemModule());
        SystemManager.RegisterModule(new LifeSupportModule());
        SystemManager.RegisterModule(new HeatModule());
        SystemManager.RegisterModule(new CommunicationsModule());
        SystemManager.RegisterModule(new NavigationModule());
    }

    private void EvaluateLoseConditions(float delta)
    {
        PowerSystemModule power = SystemManager.GetModule<PowerSystemModule>();

        if (power != null && power.CurrentCharge <= 0f && power.IsInPowerDeficit)
        {
            _powerFailureTimer += delta;
            if (_powerFailureTimer >= PowerFailureGraceSeconds)
            {
                SetGameOver(GameOverReason.PowerFailure, "Station batteries depleted and power demand is unsustainable.");
                return;
            }
        }
        else
        {
            _powerFailureTimer = 0f;
        }

        ModuleStatus failedCritical = CurrentSystemStatus.Modules
            .FirstOrDefault(m => m.Criticality == SystemCriticality.Critical && m.IsFailed && m.Name != "Power");

        if (failedCritical != null)
        {
            SetGameOver(GameOverReason.LifeSupportFailure, "Critical system failure: life support collapsed.");
            return;
        }

        if (OrbitManager.OrbitStability <= 0f)
        {
            SetGameOver(GameOverReason.OrbitFailure, "Orbit failure: station stability collapsed.");
        }
    }

    private void ApplyFailureConsequences(float delta)
    {
        ModuleStatus heat = CurrentSystemStatus.Modules.FirstOrDefault(m => m.Name == "Heat");
        ModuleStatus navigation = CurrentSystemStatus.Modules.FirstOrDefault(m => m.Name == "Navigation");
        ModuleStatus communications = CurrentSystemStatus.Modules.FirstOrDefault(m => m.Name == "Communications");

        bool heatFailed = heat is { IsFailed: true } && !heat.IsManuallyDisabled;
        bool navigationFailed = navigation is { IsFailed: true } && !navigation.IsManuallyDisabled;
        bool communicationsFailed = communications is { IsFailed: true } && !communications.IsManuallyDisabled;

        _pendingDemandPenaltyMultiplier = heatFailed ? HeatFailureDemandPenalty : 1f;

        if (heatFailed)
        {
            OrbitManager.ModifyOrbitStability(-HeatFailureOrbitPenaltyPerSecond * delta);
        }

        if (navigationFailed)
        {
            OrbitManager.ModifyOrbitStability(-NavigationFailureOrbitPenaltyPerSecond * delta);
        }

        if (communicationsFailed)
        {
            OrbitManager.ModifyOrbitStability(-CommunicationsFailureOrbitPenaltyPerSecond * delta);
        }
    }

    private void SetGameOver(GameOverReason reason, string message)
    {
        if (State == GameState.GameOver)
        {
            return;
        }

        State = GameState.GameOver;
        GameOverReason = reason;
        GameOverMessage = message;
        IsGameRunning = false;
    }

    /// <summary>
    /// Gets the latest system snapshot.
    /// </summary>
    /// <returns>Current system snapshot.</returns>
    public SystemStatusSnapshot GetSystemSnapshot()
    {
        return CurrentSystemStatus;
    }

    /// <summary>
    /// Gets the latest orbit snapshot.
    /// </summary>
    /// <returns>Current orbit snapshot.</returns>
    public OrbitStatusSnapshot GetOrbitSnapshot()
    {
        return CurrentOrbitStatus;
    }

    /// <summary>
    /// Gets currently applicable upgrades.
    /// </summary>
    /// <returns>Applicable upgrades after rule checks.</returns>
    public IReadOnlyList<UpgradeData> GetAvailableUpgrades()
    {
        return UpgradeManager.GetApplicableUpgrades(SystemManager);
    }

    /// <summary>
    /// Gets already applied upgrades.
    /// </summary>
    /// <returns>Applied upgrade list.</returns>
    public IReadOnlyList<UpgradeData> GetAppliedUpgrades()
    {
        return UpgradeManager.AppliedUpgrades;
    }

    /// <summary>
    /// Gets currently selected upgrade for debug interaction layer.
    /// </summary>
    /// <returns>Selected upgrade or null when none available.</returns>
    public UpgradeData GetSelectedUpgrade()
    {
        IReadOnlyList<UpgradeData> upgrades = GetAvailableUpgrades();
        if (upgrades.Count == 0)
        {
            return null;
        }

        _selectedUpgradeIndex = ((_selectedUpgradeIndex % upgrades.Count) + upgrades.Count) % upgrades.Count;
        return upgrades[_selectedUpgradeIndex];
    }

    /// <summary>
    /// Selects next available upgrade.
    /// </summary>
    /// <returns>Selected upgrade or null when none available.</returns>
    public UpgradeData SelectNextUpgrade()
    {
        IReadOnlyList<UpgradeData> upgrades = GetAvailableUpgrades();
        if (upgrades.Count == 0)
        {
            LastActionMessage = "No upgrades available.";
            return null;
        }

        _selectedUpgradeIndex = (_selectedUpgradeIndex + 1) % upgrades.Count;
        UpgradeData selected = upgrades[_selectedUpgradeIndex];
        LastActionMessage = $"Selected upgrade: {selected.Name}";
        return selected;
    }

    /// <summary>
    /// Selects previous available upgrade.
    /// </summary>
    /// <returns>Selected upgrade or null when none available.</returns>
    public UpgradeData SelectPreviousUpgrade()
    {
        IReadOnlyList<UpgradeData> upgrades = GetAvailableUpgrades();
        if (upgrades.Count == 0)
        {
            LastActionMessage = "No upgrades available.";
            return null;
        }

        _selectedUpgradeIndex = (_selectedUpgradeIndex - 1 + upgrades.Count) % upgrades.Count;
        UpgradeData selected = upgrades[_selectedUpgradeIndex];
        LastActionMessage = $"Selected upgrade: {selected.Name}";
        return selected;
    }

    /// <summary>
    /// Applies currently selected upgrade.
    /// </summary>
    /// <returns>True when applied successfully.</returns>
    public bool ApplySelectedUpgrade()
    {
        UpgradeData selected = GetSelectedUpgrade();
        if (selected == null)
        {
            LastActionMessage = "No upgrade selected.";
            return false;
        }

        bool applied = UpgradeManager.ApplyUpgrade(selected.Id, SystemManager, OrbitManager, out string message);
        LastActionMessage = message;
        CurrentSystemStatus = SystemManager.GetSnapshot();
        CurrentOrbitStatus = OrbitManager.CreateSnapshot();
        return applied;
    }

    /// <summary>
    /// Disables a module for sacrifice/power-priority decisions.
    /// Critical systems cannot be manually disabled.
    /// </summary>
    /// <param name="moduleName">Module display name.</param>
    /// <returns>True when module disabled successfully.</returns>
    public bool DisableModule(string moduleName)
    {
        var module = SystemManager.FindModule(moduleName);
        if (module == null)
        {
            LastActionMessage = $"Module '{moduleName}' not found.";
            return false;
        }

        if (module.Criticality == SystemCriticality.Critical)
        {
            LastActionMessage = $"Cannot disable critical module '{module.Name}'.";
            return false;
        }

        module.SetManuallyDisabled(true);
        LastActionMessage = $"Module disabled: {module.Name}";
        return true;
    }

    /// <summary>
    /// Re-enables a previously manually disabled module.
    /// </summary>
    /// <param name="moduleName">Module display name.</param>
    /// <returns>True when module enabled successfully.</returns>
    public bool EnableModule(string moduleName)
    {
        var module = SystemManager.FindModule(moduleName);
        if (module == null)
        {
            LastActionMessage = $"Module '{moduleName}' not found.";
            return false;
        }

        module.SetManuallyDisabled(false);
        LastActionMessage = $"Module enabled: {module.Name}";
        return true;
    }

    /// <summary>
    /// Exports current runtime state to save DTO.
    /// </summary>
    public SaveGameData CreateSaveData()
    {
        return new SaveGameData
        {
            GameState = State.ToString(),
            GameOverReason = GameOverReason.ToString(),
            GameOverMessage = GameOverMessage,
            IsGameRunning = IsGameRunning,
            SessionElapsedSeconds = _sessionElapsedSeconds,
            PendingDemandPenaltyMultiplier = _pendingDemandPenaltyMultiplier,
            PowerFailureTimer = _powerFailureTimer,
            Orbit = OrbitManager.CreateSaveData(),
            Modules = SystemManager.CreateModuleSaveData(),
            Upgrades = UpgradeManager.CreateSaveData(),
            Metadata = new SaveMetadata
            {
                DisplayName = "Orbit Run",
                GameState = State.ToString(),
                BatteryPercent = CurrentSystemStatus.BatteryPercent,
                OrbitProgressPercent = CurrentOrbitStatus.NormalizedOrbitProgress * 100f,
                OrbitStability = CurrentOrbitStatus.OrbitStability
            }
        };
    }

    /// <summary>
    /// Loads runtime state from save DTO.
    /// </summary>
    public bool LoadFromSaveData(SaveGameData data, out string error)
    {
        error = string.Empty;
        if (data == null)
        {
            error = "Save data is null.";
            return false;
        }

        RegisterDefaultModulesIfNeeded();
        UpgradeManager.LoadFromJson(_upgradeConfigPath);
        UpgradeManager.LoadFromSaveData(data.Upgrades);

        OrbitManager.LoadFromSaveData(data.Orbit);
        SystemManager.LoadFromModuleSaveData(data.Modules);

        if (!System.Enum.TryParse(data.GameState, out GameState parsedState))
        {
            parsedState = GameState.Running;
        }

        if (!System.Enum.TryParse(data.GameOverReason, out GameOverReason parsedReason))
        {
            parsedReason = GameOverReason.None;
        }

        State = parsedState;
        GameOverReason = parsedReason;
        GameOverMessage = data.GameOverMessage ?? string.Empty;
        IsGameRunning = data.IsGameRunning && State != GameState.GameOver;
        _sessionElapsedSeconds = data.SessionElapsedSeconds;
        _pendingDemandPenaltyMultiplier = data.PendingDemandPenaltyMultiplier < 1f ? 1f : data.PendingDemandPenaltyMultiplier;
        _powerFailureTimer = data.PowerFailureTimer;

        SystemManager.SetDemandPenaltyMultiplier(_pendingDemandPenaltyMultiplier);
        CurrentSystemStatus = SystemManager.TickAll(0f, OrbitManager.IsDaytime);
        CurrentOrbitStatus = OrbitManager.CreateSnapshot();
        LastActionMessage = $"Loaded save slot '{data.Metadata?.SlotId}'.";
        return true;
    }
}
