using Godot;
using ZeroDayOrbit.Core.Save;
using ZeroDayOrbit.Infrastructure;

/// <summary>
/// Main menu scene script that handles title animation and button-driven scene flow.
/// </summary>
public partial class TitleScreen : BaseTitleScreen
{
	[Export(PropertyHint.File, "*.tscn")]
	private string _gameplayScenePath = ScenePaths.Gameplay;

	[Export]
	private bool _deferSceneChange = true;

	[Export]
	private NodePath _startButtonPath = "MenuButtons/NewGameButton";

	[Export]
	private NodePath _loadButtonPath = "MenuButtons/LoadGameButton";

	[Export]
	private NodePath _settingsButtonPath = "MenuButtons/SettingsButton";

	[Export]
	private NodePath _quitButtonPath = "MenuButtons/QuitButton";

	[Export]
	private NodePath _loadPanelPath = "LoadPanel";

	[Export]
	private NodePath _saveListPath = "LoadPanel/Panel/Margin/VBox/SaveList";

	[Export]
	private NodePath _loadBackButtonPath = "LoadPanel/Panel/Margin/VBox/BackButton";

	[Export]
	private NodePath _loadStatusLabelPath = "LoadPanel/Panel/Margin/VBox/StatusLabel";

	private VBoxContainer _menuButtons;
	private Button _newGameButton;
	private Button _loadGameButton;
	private Button _settingsButton;
	private Button _quitButton;
	private Control _loadPanel;
	private VBoxContainer _saveList;
	private Button _loadBackButton;
	private Label _loadStatusLabel;

	/// <summary>
	/// Returns the menu title text shown in the 3D viewport.
	/// </summary>

	protected override string GetTitleText()
	{
		return "Zero Day Orbit";
	}
	
	/// <summary>
	/// Returns the start position for the title (offscreen below view).
	/// </summary>
	protected override Vector3 GetTitleStartPosition()
	{
		return new Vector3(0, -8.0f, 2); // Start below the camera view
	}
	
	/// <summary>
	/// Returns the end position for the title near the top edge of Earth.
	/// </summary>
	protected override Vector3 GetTitleEndPosition()
	{
		return new Vector3(0, 2.05f, 2); // Settle near the top of the Earth model
	}

	/// <summary>
	/// Applies ease-out motion so the title slows as it reaches final position.
	/// </summary>
	/// <param name="t">Linear animation progress in range 0-1.</param>
	/// <returns>Eased progress value.</returns>
	protected override float ApplyTitleSlideEasing(float t)
	{
		t = Mathf.Clamp(t, 0.0f, 1.0f);
		return 1.0f - Mathf.Pow(1.0f - t, 3.0f); // Ease-out cubic for a soft slowdown
	}
	
	/// <summary>
	/// Reveals menu options once title animation is finished.
	/// </summary>
	protected override void OnAnimationComplete()
	{
		FadeInButtons();
	}
	
	/// <summary>
	/// Initializes visual state and button signal wiring.
	/// </summary>
	protected override void InitializeScene()
	{
		base.InitializeScene();

		_menuButtons = GetNode<VBoxContainer>("MenuButtons");
		_newGameButton = ResolveButton(_startButtonPath, "MenuButtons/NewGameButton");
		_loadGameButton = ResolveButton(_loadButtonPath, "MenuButtons/LoadGameButton");
		_settingsButton = ResolveButton(_settingsButtonPath, "MenuButtons/SettingsButton");
		_quitButton = ResolveButton(_quitButtonPath, "MenuButtons/QuitButton");
		_loadPanel = GetNodeOrNull<Control>(_loadPanelPath);
		_saveList = GetNodeOrNull<VBoxContainer>(_saveListPath);
		_loadBackButton = GetNodeOrNull<Button>(_loadBackButtonPath);
		_loadStatusLabel = GetNodeOrNull<Label>(_loadStatusLabelPath);

		_menuButtons.Modulate = new Color(1, 1, 1, 0);
		SetButtonHidden(_newGameButton);
		SetButtonHidden(_loadGameButton);
		SetButtonHidden(_settingsButton);
		SetButtonHidden(_quitButton);

		if (_loadPanel != null)
		{
			_loadPanel.Visible = false;
		}

		WireButtonSignals();
		
		// Force station to reset to its start position first, then move to orbit
		Aabb stationBounds = CalculateBounds(_stationNode);
		Vector3 size = stationBounds.Size;
		float maxDimension = Mathf.Max(size.X, Mathf.Max(size.Y, size.Z));
		
		float earthTargetSize = 3.5f;
		float stationScaleMultiplier = (earthTargetSize / 8.0f) * (1.0f / maxDimension);
		Vector3 stationCenter = stationBounds.GetCenter();
		float earthRadius = earthTargetSize / 2.0f;
		
		// Reset to orbit position
		_stationOrbitRadius = earthRadius * 1.3f;
		_stationOrbitHeight = earthRadius * 0.05f;
		_stationOrbitAngle = Mathf.DegToRad(-90); // Start at left side of orbit
		
		Vector3 orbitPos = new Vector3(
			Mathf.Sin(_stationOrbitAngle) * _stationOrbitRadius,
			_stationOrbitHeight,
			Mathf.Cos(_stationOrbitAngle) * _stationOrbitRadius
		);
		_stationNode.Position = orbitPos + (-stationCenter * stationScaleMultiplier);
		_stationVisible = true;
	}

	/// <summary>
	/// Resolves a button using exported path first, then fallback path.
	/// </summary>
	/// <param name="exportedPath">Configured path from inspector.</param>
	/// <param name="fallbackPath">Fallback path based on scene naming convention.</param>
	/// <returns>Resolved button node or null when unavailable.</returns>
	private Button ResolveButton(NodePath exportedPath, string fallbackPath)
	{
		if (exportedPath != null && !exportedPath.IsEmpty)
		{
			Button configured = GetNodeOrNull<Button>(exportedPath);
			if (configured != null)
			{
				return configured;
			}
		}

		return GetNodeOrNull<Button>(fallbackPath);
	}

	/// <summary>
	/// Wires available button pressed signals to lightweight handlers.
	/// </summary>
	private void WireButtonSignals()
	{
		if (_newGameButton != null)
		{
			_newGameButton.Pressed += OnStartGamePressed;
		}

		if (_loadGameButton != null)
		{
			_loadGameButton.Pressed += OnLoadGamePressed;
		}

		if (_settingsButton != null)
		{
			_settingsButton.Pressed += OnSettingsPressed;
		}

		if (_quitButton != null)
		{
			_quitButton.Pressed += OnQuitPressed;
		}

		if (_loadBackButton != null)
		{
			_loadBackButton.Pressed += HideLoadPanel;
		}
	}

	/// <summary>
	/// Handles Start Game flow and loads gameplay scene.
	/// </summary>
	private void OnStartGamePressed()
	{
		GameSessionBootstrap.ClearPendingLoadSlot();
		SceneNavigator.ChangeScene(this, _gameplayScenePath, _deferSceneChange);
	}

	/// <summary>
	/// Placeholder for future Continue/Load flow.
	/// </summary>
	private void OnLoadGamePressed()
	{
		ShowLoadPanel();
	}

	/// <summary>
	/// Placeholder for future settings flow.
	/// </summary>
	private void OnSettingsPressed()
	{
		GD.Print("Settings is not implemented yet.");
	}

	/// <summary>
	/// Quits the application when running outside the editor.
	/// </summary>
	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	/// <summary>
	/// Sets a button to fully transparent when the button exists.
	/// </summary>
	/// <param name="button">Button to hide before intro fade.</param>
	private static void SetButtonHidden(Button button)
	{
		if (button != null)
		{
			button.Modulate = new Color(1, 1, 1, 0);
		}
	}

	/// <summary>
	/// Fades in the button stack after the title settles.
	/// </summary>
	private void FadeInButtons()
	{
		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(_menuButtons, "modulate:a", 1.0f, 0.35f);
		TweenButtonAlpha(tween, _newGameButton, 0.45f);
		TweenButtonAlpha(tween, _loadGameButton, 0.45f);
		TweenButtonAlpha(tween, _settingsButton, 0.45f);
		TweenButtonAlpha(tween, _quitButton, 0.45f);
	}

	/// <summary>
	/// Queues alpha fade for a button if it exists.
	/// </summary>
	/// <param name="tween">Tween used for sequencing.</param>
	/// <param name="button">Button to fade in.</param>
	/// <param name="duration">Fade duration in seconds.</param>
	private static void TweenButtonAlpha(Tween tween, Button button, float duration)
	{
		if (button != null)
		{
			tween.TweenProperty(button, "modulate:a", 1.0f, duration);
		}
	}

	private void ShowLoadPanel()
	{
		if (_loadPanel == null)
		{
			return;
		}

		BuildSaveSlotButtons();
		_loadPanel.Visible = true;
	}

	private void HideLoadPanel()
	{
		if (_loadPanel != null)
		{
			_loadPanel.Visible = false;
		}
	}

	private void BuildSaveSlotButtons()
	{
		if (_saveList == null)
		{
			return;
		}

		foreach (Node child in _saveList.GetChildren())
		{
			child.QueueFree();
		}

		var saves = SaveManager.ListSaves();
		if (saves.Count == 0)
		{
			if (_loadStatusLabel != null)
			{
				_loadStatusLabel.Text = "No save files found.";
			}

			return;
		}

		if (_loadStatusLabel != null)
		{
			_loadStatusLabel.Text = $"{saves.Count} save(s) available.";
		}

		foreach (SaveMetadata save in saves)
		{
			string savedAt = string.IsNullOrWhiteSpace(save.SavedAtUtc) ? "unknown" : save.SavedAtUtc;
			var button = new Button
			{
				Text = $"{save.SlotId}  |  {save.GameState}  |  Bat {save.BatteryPercent:F1}%  |  Orbit {save.OrbitProgressPercent:F0}%  |  {savedAt}",
				CustomMinimumSize = new Vector2(0, 56)
			};

			button.Pressed += () => LoadSelectedSlot(save.SlotId);
			_saveList.AddChild(button);
		}
	}

	private void LoadSelectedSlot(string slotId)
	{
		GameSessionBootstrap.SetPendingLoadSlot(slotId);
		SceneNavigator.ChangeScene(this, _gameplayScenePath, _deferSceneChange);
	}
}
