using Godot;
using ZeroDayOrbit.Core.Controllers;
using ZeroDayOrbit.Core.Managers;
using ZeroDayOrbit.Core.Models;
using ZeroDayOrbit.Core.Save;
using ZeroDayOrbit.Infrastructure;
using ZeroDayOrbit.UI;

namespace ZeroDayOrbit.Scenes;

/// <summary>
/// Thin Godot scene entry point that delegates simulation to <see cref="GameController"/>.
/// </summary>
public partial class GameRoot : Control
{
    [Export]
    private bool _autoStart = true;

    [Export(PropertyHint.File, "*.tscn")]
    private string _mainMenuScenePath = ScenePaths.MainMenu;

    [Export(PropertyHint.Range, "60,600,5")]
    private float _orbitDurationSeconds = 180f;

    [Export]
    private bool _enableDebugLogging = true;

    [Export]
    private bool _enableDebugPanel = true;

    [Export]
    private NodePath _debugLabelPath = "DebugCanvas/DebugPanel/MarginContainer/DebugLabel";

    [Export(PropertyHint.Range, "0.2,10,0.1")]
    private float _debugLogIntervalSeconds = 1.5f;

    [Export]
    private Key _toggleDebugPanelKey = Key.F3;

    [Export]
    private Key _togglePauseMenuKey = Key.Escape;

    [Export]
    private Key _previousUpgradeKey = Key.Q;

    [Export]
    private Key _nextUpgradeKey = Key.E;

    [Export]
    private Key _applyUpgradeKey = Key.R;

    [Export]
    private NodePath _spaceEnvPath = "SubViewport/World3D";

    [Export]
    private NodePath _debugHudPath = "DebugCanvas/DebugHud";

    [Export]
    private NodePath _pauseMenuPath = "DebugCanvas/PauseMenu";

    private GameController _gameController;
    private SpaceEnvironmentController _spaceEnv;
    private DebugHudController _debugHud;
    private PauseMenuController _pauseMenu;
    private float _debugLogTimer;
    private bool _lastIsDaytime;
    private GameState _lastState;
    private RichTextLabel _debugLabel;

    /// <summary>
    /// Gets the active gameplay controller instance.
    /// </summary>
    public GameController Controller => _gameController;

    /// <summary>
    /// Creates and starts the gameplay controller when the scene becomes active.
    /// </summary>
    public override void _Ready()
    {
        _gameController = new GameController(orbitManager: new OrbitManager(_orbitDurationSeconds));
        _spaceEnv = GetNodeOrNull<SpaceEnvironmentController>(_spaceEnvPath);
        _debugHud = GetNodeOrNull<DebugHudController>(_debugHudPath);
        _pauseMenu = GetNodeOrNull<PauseMenuController>(_pauseMenuPath);
        _debugLabel = GetNodeOrNull<RichTextLabel>(_debugLabelPath);

        _debugHud?.Bind(_gameController);
        _spaceEnv?.SyncOrbit(0f, isDaytime: true);

        if (_pauseMenu != null)
        {
            _pauseMenu.ResumeRequested += ResumeSimulation;
            _pauseMenu.SaveRequested += OnPauseMenuSaveRequested;
            _pauseMenu.ExitToMainMenuRequested += OnPauseMenuExitRequested;
        }

        if (_debugLabel != null)
        {
            _debugLabel.Visible = _enableDebugPanel;
        }

        _lastState = _gameController.State;

        if (_autoStart)
        {
            StartSimulation();
        }
    }

    /// <summary>
    /// Forwards frame delta to the game controller update loop.
    /// </summary>
    /// <param name="delta">Elapsed frame time in seconds.</param>
    public override void _Process(double delta)
    {
        _gameController?.UpdateGame((float)delta);

        if (_gameController == null)
        {
            return;
        }

        OrbitStatusSnapshot currentOrbit = _gameController.CurrentOrbitStatus;
        _spaceEnv?.SyncOrbit(currentOrbit.NormalizedOrbitProgress, currentOrbit.IsDaytime);
        _debugHud?.Refresh();

        if (_enableDebugPanel)
        {
            UpdateDebugPanel();
        }

        if (!_enableDebugLogging)
        {
            return;
        }

        if (currentOrbit.IsDaytime != _lastIsDaytime)
        {
            GD.Print($"[Orbit] Transition -> {(currentOrbit.IsDaytime ? "DAY" : "NIGHT")}");
            _lastIsDaytime = currentOrbit.IsDaytime;
        }

        if (_gameController.State != _lastState)
        {
            _lastState = _gameController.State;
            GD.Print($"[Game] State changed -> {_gameController.State} ({_gameController.GameOverReason}) {_gameController.GameOverMessage}");
        }

        _debugLogTimer += (float)delta;
        if (_debugLogTimer >= _debugLogIntervalSeconds)
        {
            _debugLogTimer = 0f;
            PrintDebugSnapshot();
        }
    }

    /// <summary>
    /// Handles local debug shortcuts.
    /// </summary>
    /// <param name="@event">Input event to process.</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        if (keyEvent.Keycode == _toggleDebugPanelKey)
        {
            ToggleDebugPanel();
            return;
        }

        if (keyEvent.Keycode == _togglePauseMenuKey)
        {
            TogglePauseMenu();
            return;
        }

        if (keyEvent.Keycode == _previousUpgradeKey)
        {
            _gameController.SelectPreviousUpgrade();
            GD.Print($"[Upgrade] {_gameController.LastActionMessage}");
            return;
        }

        if (keyEvent.Keycode == _nextUpgradeKey)
        {
            _gameController.SelectNextUpgrade();
            GD.Print($"[Upgrade] {_gameController.LastActionMessage}");
            return;
        }

        if (keyEvent.Keycode == _applyUpgradeKey)
        {
            _gameController.ApplySelectedUpgrade();
            GD.Print($"[Upgrade] {_gameController.LastActionMessage}");
        }
    }

    /// <summary>
    /// Starts simulation if it is not already running.
    /// </summary>
    public void StartSimulation()
    {
        if (_gameController == null || _gameController.IsGameRunning)
        {
            return;
        }

        string pendingSlot = GameSessionBootstrap.PendingLoadSlotId;
        string loadError = string.Empty;
        if (!string.IsNullOrWhiteSpace(pendingSlot) && TryLoadGame(pendingSlot, out loadError))
        {
            GameSessionBootstrap.ClearPendingLoadSlot();
            GD.Print($"[Save] Loaded slot '{pendingSlot}'");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(pendingSlot))
            {
                GD.PushWarning($"[Save] Could not load slot '{pendingSlot}': {loadError}. Starting new game.");
                GameSessionBootstrap.ClearPendingLoadSlot();
            }

            _gameController.StartGame();
        }

        _lastIsDaytime = _gameController.CurrentOrbitStatus.IsDaytime;
        _spaceEnv?.SyncOrbit(
            _gameController.CurrentOrbitStatus.NormalizedOrbitProgress,
            _gameController.CurrentOrbitStatus.IsDaytime);
        _debugHud?.Refresh();
    }

    /// <summary>
    /// Pauses scene tree processing. Placeholder for future pause menu integration.
    /// </summary>
    public void PauseSimulation()
    {
        if (_pauseMenu != null)
        {
            _pauseMenu.Visible = true;
            _pauseMenu.SetStatus("Paused");
        }

        GetTree().Paused = true;
    }

    /// <summary>
    /// Resumes scene tree processing.
    /// </summary>
    public void ResumeSimulation()
    {
        GetTree().Paused = false;

        if (_pauseMenu != null)
        {
            _pauseMenu.Visible = false;
            _pauseMenu.SetStatus(string.Empty);
        }
    }

    /// <summary>
    /// Returns to main menu scene.
    /// </summary>
    public void ReturnToMainMenu()
    {
        GetTree().Paused = false;
        SceneNavigator.ChangeScene(this, _mainMenuScenePath, deferred: true);
    }

    private void TogglePauseMenu()
    {
        if (_pauseMenu == null)
        {
            return;
        }

        if (GetTree().Paused)
        {
            ResumeSimulation();
        }
        else
        {
            PauseSimulation();
        }
    }

    private void OnPauseMenuSaveRequested(string slotId)
    {
        string resolvedSlot = string.IsNullOrWhiteSpace(slotId) ? SaveManager.CreateAutoSlotId() : slotId.Trim();

        if (SaveCurrentGame(resolvedSlot, out string error))
        {
            _pauseMenu?.SetStatus($"Saved to slot: {resolvedSlot}");
        }
        else
        {
            _pauseMenu?.SetStatus($"Save failed: {error}");
        }
    }

    private void OnPauseMenuExitRequested()
    {
        ReturnToMainMenu();
    }

    private bool SaveCurrentGame(string slotId, out string error)
    {
        SaveGameData data = _gameController.CreateSaveData();
        return SaveManager.SaveToSlot(slotId, data, out error);
    }

    private bool TryLoadGame(string slotId, out string error)
    {
        error = string.Empty;

        if (!SaveManager.TryLoadFromSlot(slotId, out SaveGameData data, out error))
        {
            return false;
        }

        return _gameController.LoadFromSaveData(data, out error);
    }

    private void PrintDebugSnapshot()
    {
        SystemStatusSnapshot system = _gameController.CurrentSystemStatus;
        OrbitStatusSnapshot orbit = _gameController.CurrentOrbitStatus;

        GD.Print(
            $"[Sim] {(orbit.IsDaytime ? "DAY" : "NIGHT")} " +
            $"Orbit={(orbit.NormalizedOrbitProgress * 100f):F0}% " +
            $"Battery={system.BatteryPercent:F1}% " +
            $"Solar={system.SolarGeneration:F2} " +
            $"Demand={system.TotalPowerDraw:F2} " +
            $"Net={system.NetPowerBalance:F2} " +
            $"Deficit={system.IsPowerDeficit}"
        );

        foreach (ModuleStatus module in system.Modules)
        {
            GD.Print($"[Module] {module.Name} online={module.IsOnline} powered={module.IsPowered} health={module.HealthPercent:F0}% {module.Detail}");
        }
    }

    private void ToggleDebugPanel()
    {
        _enableDebugPanel = !_enableDebugPanel;

        if (_debugLabel != null)
        {
            _debugLabel.Visible = _enableDebugPanel;
        }

        GD.Print($"[Debug] Panel {(_enableDebugPanel ? "enabled" : "disabled")} (key: {_toggleDebugPanelKey})");
    }

    private void UpdateDebugPanel()
    {
        if (_debugLabel == null)
        {
            return;
        }

        SystemStatusSnapshot system = _gameController.CurrentSystemStatus;
        OrbitStatusSnapshot orbit = _gameController.CurrentOrbitStatus;

        var text =
            "[b]Station Debug[/b]\n" +
            $"State: {_gameController.State} ({_gameController.GameOverReason})\n" +
            $"Phase: {(orbit.IsDaytime ? "DAY" : "NIGHT")}  Orbit: {(orbit.NormalizedOrbitProgress * 100f):F0}%\n" +
            $"Stability: {orbit.OrbitStability:F1}%\n" +
            $"Battery: {system.BatteryPercent:F1}%\n" +
            $"Solar: {system.SolarGeneration:F2}  Demand: {system.TotalPowerDraw:F2}\n" +
            $"Net: {system.NetPowerBalance:F2}  Deficit: {system.IsPowerDeficit}\n\n" +
            "[b]Modules[/b]\n";

        foreach (ModuleStatus module in system.Modules)
        {
            text += $"- {module.Name}: online={module.IsOnline}, powered={module.IsPowered}, health={module.HealthPercent:F0}%\n";
        }

        if (_gameController.State == GameState.GameOver)
        {
            text += $"\n[color=red][b]Game Over:[/b] {_gameController.GameOverMessage}[/color]";
        }

        _debugLabel.Text = text;
    }
}
