using System.Text;
using System.Collections.Generic;
using Godot;
using ZeroDayOrbit.Core.Controllers;
using ZeroDayOrbit.Core.Models;
using ZeroDayOrbit.Core.Interfaces;

namespace ZeroDayOrbit.UI;

/// <summary>
/// Lightweight debug HUD that renders orbit, battery, and module health state.
/// </summary>
public partial class DebugHudController : Control
{
    [Export]
    private NodePath _batteryBarPath = "Panel/Margin/VBox/BatteryBar";

    [Export]
    private NodePath _batteryLabelPath = "Panel/Margin/VBox/BatteryLabel";

    [Export]
    private NodePath _orbitLabelPath = "Panel/Margin/VBox/OrbitLabel";

    [Export]
    private NodePath _phaseLabelPath = "Panel/Margin/VBox/PhaseLabel";

    [Export]
    private NodePath _modulesLabelPath = "Panel/Margin/VBox/ModulesLabel";

    [Export]
    private NodePath _toggleBarsButtonPath = "Panel/Margin/VBox/Controls/ToggleBarsButton";

    [Export]
    private NodePath _toggleColorBarsButtonPath = "Panel/Margin/VBox/Controls/ToggleColorBarsButton";

    [Export]
    private NodePath _moduleButtonsPath = "Panel/Margin/VBox/ModuleButtons";

    [Export]
    private NodePath _upgradeSummaryLabelPath = "Panel/Margin/VBox/UpgradeSummaryLabel";

    [Export]
    private NodePath _upgradeAppliedLabelPath = "Panel/Margin/VBox/UpgradeAppliedLabel";

    [Export]
    private NodePath _upgradePrevButtonPath = "Panel/Margin/VBox/UpgradeControls/PrevUpgradeButton";

    [Export]
    private NodePath _upgradeNextButtonPath = "Panel/Margin/VBox/UpgradeControls/NextUpgradeButton";

    [Export]
    private NodePath _upgradeApplyButtonPath = "Panel/Margin/VBox/UpgradeControls/ApplyUpgradeButton";

    private ProgressBar _batteryBar;
    private Label _batteryLabel;
    private Label _orbitLabel;
    private Label _phaseLabel;
    private RichTextLabel _modulesLabel;
    private Label _upgradeSummaryLabel;
    private Label _upgradeAppliedLabel;
    private Button _toggleBarsButton;
    private Button _toggleColorBarsButton;
    private Button _upgradePrevButton;
    private Button _upgradeNextButton;
    private Button _upgradeApplyButton;
    private VBoxContainer _moduleButtons;
    private GameController _controller;
    private bool _showMiniBars = true;
    private bool _useColorizedBars = true;
    private readonly Dictionary<string, Button> _moduleToggleButtons = new();

    /// <inheritdoc />
    public override void _Ready()
    {
        _batteryBar = GetNodeOrNull<ProgressBar>(_batteryBarPath);
        _batteryLabel = GetNodeOrNull<Label>(_batteryLabelPath);
        _orbitLabel = GetNodeOrNull<Label>(_orbitLabelPath);
        _phaseLabel = GetNodeOrNull<Label>(_phaseLabelPath);
        _modulesLabel = GetNodeOrNull<RichTextLabel>(_modulesLabelPath);
        _upgradeSummaryLabel = GetNodeOrNull<Label>(_upgradeSummaryLabelPath);
        _upgradeAppliedLabel = GetNodeOrNull<Label>(_upgradeAppliedLabelPath);
        _toggleBarsButton = GetNodeOrNull<Button>(_toggleBarsButtonPath);
        _toggleColorBarsButton = GetNodeOrNull<Button>(_toggleColorBarsButtonPath);
        _upgradePrevButton = GetNodeOrNull<Button>(_upgradePrevButtonPath);
        _upgradeNextButton = GetNodeOrNull<Button>(_upgradeNextButtonPath);
        _upgradeApplyButton = GetNodeOrNull<Button>(_upgradeApplyButtonPath);
        _moduleButtons = GetNodeOrNull<VBoxContainer>(_moduleButtonsPath);

        if (_toggleBarsButton != null)
        {
            _toggleBarsButton.Pressed += OnToggleBarsPressed;
        }

        if (_toggleColorBarsButton != null)
        {
            _toggleColorBarsButton.Pressed += OnToggleColorBarsPressed;
        }

        if (_upgradePrevButton != null)
        {
            _upgradePrevButton.Pressed += OnSelectPreviousUpgradePressed;
        }

        if (_upgradeNextButton != null)
        {
            _upgradeNextButton.Pressed += OnSelectNextUpgradePressed;
        }

        if (_upgradeApplyButton != null)
        {
            _upgradeApplyButton.Pressed += OnApplyUpgradePressed;
        }

        RefreshButtonText();
    }

    /// <summary>
    /// Binds the HUD to the active game controller.
    /// </summary>
    /// <param name="controller">Gameplay controller instance.</param>
    public void Bind(GameController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// Updates all HUD text, bars, and colors from latest snapshots.
    /// </summary>
    public void Refresh()
    {
        if (_controller == null)
        {
            return;
        }

        SystemStatusSnapshot system = _controller.GetSystemSnapshot();
        OrbitStatusSnapshot orbit = _controller.GetOrbitSnapshot();

        EnsureModuleToggleButtons();
        RefreshModuleToggleButtonLabels();

        UpdateBattery(system);
        UpdateOrbit(orbit);
        UpdateModules(system);
        UpdateUpgrades();
    }

    private void UpdateBattery(SystemStatusSnapshot system)
    {
        if (_batteryBar != null)
        {
            _batteryBar.Value = Mathf.Clamp(system.BatteryPercent, 0f, 100f);
            _batteryBar.Modulate = GetBatteryColor(system.BatteryPercent);
        }

        if (_batteryLabel != null)
        {
            _batteryLabel.Text =
                $"Battery {system.BatteryPercent:F1}%  Gen {system.SolarGeneration:F2}  Draw {system.TotalPowerDraw:F2}  Net {system.NetPowerBalance:F2}";
            _batteryLabel.Modulate = GetBatteryColor(system.BatteryPercent);
        }
    }

    private void UpdateOrbit(OrbitStatusSnapshot orbit)
    {
        if (_orbitLabel != null)
        {
            _orbitLabel.Text = $"Orbit {(orbit.NormalizedOrbitProgress * 100f):F0}%  Stability {orbit.OrbitStability:F1}%";
        }

        if (_phaseLabel != null)
        {
            _phaseLabel.Text = orbit.IsDaytime ? "DAYLIGHT" : "NIGHT";
            _phaseLabel.Modulate = orbit.IsDaytime
                ? new Color(0.68f, 0.84f, 1.0f, 1f)
                : new Color(0.31f, 0.43f, 0.78f, 1f);
        }
    }

    private void UpdateModules(SystemStatusSnapshot system)
    {
        if (_modulesLabel == null)
        {
            return;
        }

        bool commsFailed = system.Modules.Exists(m => m.Name == "Communications" && m.IsFailed && !m.IsManuallyDisabled);

        var text = new StringBuilder();
        text.AppendLine("[b]Systems[/b]");

        if (commsFailed)
        {
            text.AppendLine("[color=orange]Telemetry degraded: Communications offline.[/color]");
        }

        foreach (ModuleStatus status in system.Modules)
        {
            Color color = GetStatusColor(status);
            string hex = color.ToHtml(false);
            string online = status.IsOnline ? "ON" : "OFF";
            string powered = status.IsPowered ? "PWR" : "NO_PWR";
            string criticality = status.Criticality switch
            {
                SystemCriticality.Critical => "CRIT",
                SystemCriticality.Important => "IMP",
                _ => "OPT"
            };
            string state = status.IsManuallyDisabled
                ? "DISABLED"
                : status.IsFailed
                    ? "FAILED"
                    : status.HealthPercent < 65f
                        ? "DEGRADED"
                        : "HEALTHY";

            if (_showMiniBars)
            {
                string hpBar = BuildBar(status.HealthPercent, 10, color, _useColorizedBars);
                string pwrBar = BuildBar(status.IsPowered ? 100f : 0f, 6, color, _useColorizedBars);
                if (commsFailed)
                {
                    text.AppendLine($"[color=#{hex}]■[/color] {status.Name,-14} {criticality,4} {state,8}  HP {status.HealthPercent,5:F1}%");
                }
                else
                {
                    text.AppendLine(
                        $"[color=#{hex}]■[/color] {status.Name,-14} {criticality,4} {state,8} {online,3} {powered,6}  H[{hpBar}]  P[{pwrBar}]  HP {status.HealthPercent,5:F1}%  Draw {status.PowerDraw,4:F2}");
                }
            }
            else
            {
                if (commsFailed)
                {
                    text.AppendLine($"[color=#{hex}]■[/color] {status.Name,-14} {criticality,4} {state,8}  HP {status.HealthPercent,5:F1}%");
                }
                else
                {
                    text.AppendLine(
                        $"[color=#{hex}]■[/color] {status.Name,-14} {criticality,4} {state,8} {online,3} {powered,6}  HP {status.HealthPercent,5:F1}%  Draw {status.PowerDraw,4:F2}");
                }
            }
        }

        _modulesLabel.Text = text.ToString();
    }

    private static Color GetBatteryColor(float batteryPercent)
    {
        if (batteryPercent < 20f)
        {
            return new Color(0.91f, 0.31f, 0.30f, 1f);
        }

        if (batteryPercent < 50f)
        {
            return new Color(0.95f, 0.76f, 0.27f, 1f);
        }

        return new Color(0.42f, 0.85f, 0.42f, 1f);
    }

    private static Color GetStatusColor(ModuleStatus status)
    {
        if (status.IsManuallyDisabled)
        {
            return new Color(0.45f, 0.45f, 0.45f, 1f);
        }

        if (status.IsFailed)
        {
            return new Color(0.91f, 0.31f, 0.30f, 1f);
        }

        if (!status.IsOnline || !status.IsPowered)
        {
            return new Color(0.60f, 0.60f, 0.60f, 1f);
        }

        if (status.HealthPercent < 30f)
        {
            return new Color(0.91f, 0.31f, 0.30f, 1f);
        }

        if (status.HealthPercent < 65f)
        {
            return new Color(0.95f, 0.76f, 0.27f, 1f);
        }

        return new Color(0.42f, 0.85f, 0.42f, 1f);
    }

    private static string BuildBar(float percent, int width, Color color, bool useColorizedBars)
    {
        int clampedWidth = Mathf.Max(1, width);
        float p = Mathf.Clamp(percent, 0f, 100f);
        int filled = Mathf.Clamp(Mathf.RoundToInt((p / 100f) * clampedWidth), 0, clampedWidth);

        if (!useColorizedBars)
        {
            return new string('#', filled) + new string('-', clampedWidth - filled);
        }

        string colorHex = color.ToHtml(false);
        string filledPart = new string('#', filled);
        string emptyPart = new string('-', clampedWidth - filled);
        return $"[color=#{colorHex}]{filledPart}[/color][color=#555555]{emptyPart}[/color]";

    }

    private void OnToggleBarsPressed()
    {
        _showMiniBars = !_showMiniBars;
        RefreshButtonText();
        Refresh();
    }

    private void OnToggleColorBarsPressed()
    {
        _useColorizedBars = !_useColorizedBars;
        RefreshButtonText();
        Refresh();
    }

    private void RefreshButtonText()
    {
        if (_toggleBarsButton != null)
        {
            _toggleBarsButton.Text = _showMiniBars ? "Mini Bars: ON" : "Mini Bars: OFF";
        }

        if (_toggleColorBarsButton != null)
        {
            _toggleColorBarsButton.Text = _useColorizedBars ? "Color Bars: ON" : "Color Bars: OFF";
            _toggleColorBarsButton.Disabled = !_showMiniBars;
        }

    }

    private void UpdateUpgrades()
    {
        if (_controller == null)
        {
            return;
        }

        UpgradeData selected = _controller.GetSelectedUpgrade();
        IReadOnlyList<UpgradeData> available = _controller.GetAvailableUpgrades();
        IReadOnlyList<UpgradeData> applied = _controller.GetAppliedUpgrades();

        if (_upgradeSummaryLabel != null)
        {
            if (selected == null)
            {
                _upgradeSummaryLabel.Text = $"Upgrades: 0 available, {applied.Count} applied";
            }
            else
            {
                _upgradeSummaryLabel.Text =
                    $"Selected: {selected.Name} ({selected.TargetType})  Cost {selected.Cost}  Available {available.Count}";
            }
        }

        if (_upgradeAppliedLabel != null)
        {
            string latest = applied.Count > 0 ? applied[^1].Name : "None";
            _upgradeAppliedLabel.Text = $"Applied: {applied.Count}  Latest: {latest}";
        }

        if (_upgradeApplyButton != null)
        {
            _upgradeApplyButton.Disabled = selected == null;
        }
    }

    private void EnsureModuleToggleButtons()
    {
        if (_controller == null || _moduleButtons == null)
        {
            return;
        }

        IReadOnlyList<ISystemModule> modules = _controller.SystemManager.Modules;
        if (modules.Count == 0)
        {
            return;
        }

        bool rebuild = _moduleToggleButtons.Count != modules.Count;
        if (!rebuild)
        {
            foreach (ISystemModule module in modules)
            {
                if (!_moduleToggleButtons.ContainsKey(module.Name))
                {
                    rebuild = true;
                    break;
                }
            }
        }

        if (!rebuild)
        {
            return;
        }

        foreach (Node child in _moduleButtons.GetChildren())
        {
            child.QueueFree();
        }

        _moduleToggleButtons.Clear();

        foreach (ISystemModule module in modules)
        {
            ISystemModule capturedModule = module;
            var button = new Button
            {
                Text = BuildModuleButtonText(capturedModule),
                ToggleMode = false,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            button.Pressed += () =>
            {
                if (capturedModule.IsManuallyDisabled)
                {
                    _controller.EnableModule(capturedModule.Name);
                }
                else
                {
                    _controller.DisableModule(capturedModule.Name);
                }

                RefreshModuleToggleButtonLabels();
                UpdateUpgrades();
            };

            _moduleButtons.AddChild(button);
            _moduleToggleButtons[capturedModule.Name] = button;
        }
    }

    private void RefreshModuleToggleButtonLabels()
    {
        if (_controller == null || _moduleToggleButtons.Count == 0)
        {
            return;
        }

        foreach (ISystemModule module in _controller.SystemManager.Modules)
        {
            if (!_moduleToggleButtons.TryGetValue(module.Name, out Button button))
            {
                continue;
            }

            button.Text = BuildModuleButtonText(module);
            button.Modulate = module.IsManuallyDisabled
                ? new Color(0.70f, 0.70f, 0.70f, 1f)
                : new Color(0.42f, 0.85f, 0.42f, 1f);
        }
    }

    private static string BuildModuleButtonText(ISystemModule module)
    {
        return $"{module.Name}: {(module.IsManuallyDisabled ? "DISABLED" : "ACTIVE")}";
    }

    private void OnSelectPreviousUpgradePressed()
    {
        _controller?.SelectPreviousUpgrade();
        UpdateUpgrades();
    }

    private void OnSelectNextUpgradePressed()
    {
        _controller?.SelectNextUpgrade();
        UpdateUpgrades();
    }

    private void OnApplyUpgradePressed()
    {
        _controller?.ApplySelectedUpgrade();
        UpdateUpgrades();
    }
}
