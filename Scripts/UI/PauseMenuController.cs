using Godot;

namespace ZeroDayOrbit.UI;

/// <summary>
/// Minimal in-game pause menu controller.
/// </summary>
public partial class PauseMenuController : Control
{
    [Signal]
    public delegate void ResumeRequestedEventHandler();

    [Signal]
    public delegate void SaveRequestedEventHandler(string slotId);

    [Signal]
    public delegate void ExitToMainMenuRequestedEventHandler();

    [Export]
    private NodePath _resumeButtonPath = "Panel/Margin/VBox/ResumeButton";

    [Export]
    private NodePath _saveButtonPath = "Panel/Margin/VBox/SaveButton";

    [Export]
    private NodePath _exitButtonPath = "Panel/Margin/VBox/ExitButton";

    [Export]
    private NodePath _slotInputPath = "Panel/Margin/VBox/SlotInput";

    [Export]
    private NodePath _statusLabelPath = "Panel/Margin/VBox/StatusLabel";

    private Button _resumeButton;
    private Button _saveButton;
    private Button _exitButton;
    private LineEdit _slotInput;
    private Label _statusLabel;

    /// <inheritdoc />
    public override void _Ready()
    {
        _resumeButton = GetNodeOrNull<Button>(_resumeButtonPath);
        _saveButton = GetNodeOrNull<Button>(_saveButtonPath);
        _exitButton = GetNodeOrNull<Button>(_exitButtonPath);
        _slotInput = GetNodeOrNull<LineEdit>(_slotInputPath);
        _statusLabel = GetNodeOrNull<Label>(_statusLabelPath);

        if (_resumeButton != null)
        {
            _resumeButton.Pressed += () => EmitSignal(SignalName.ResumeRequested);
        }

        if (_saveButton != null)
        {
            _saveButton.Pressed += OnSavePressed;
        }

        if (_exitButton != null)
        {
            _exitButton.Pressed += () => EmitSignal(SignalName.ExitToMainMenuRequested);
        }

        Visible = false;
    }

    /// <summary>
    /// Sets status label text.
    /// </summary>
    public void SetStatus(string text)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }
    }

    private void OnSavePressed()
    {
        string slotId = _slotInput?.Text?.Trim() ?? string.Empty;
        EmitSignal(SignalName.SaveRequested, slotId);
    }
}
