using Godot;
using System.Threading.Tasks;
using ZeroDayOrbit.Infrastructure;

/// <summary>
/// Animated splash scene that transitions to the main menu after a short hold.
/// </summary>
public partial class SplashScreen : BaseTitleScreen
{
	[Export(PropertyHint.Range, "0,10,0.1")]
	private float _transitionDelaySeconds = 0.75f;

	[Export(PropertyHint.File, "*.tscn")]
	private string _nextScenePath = ScenePaths.MainMenu;

	[Export]
	private bool _allowSkip = true;

	[Export]
	private bool _deferSceneChange = true;

	private bool _transitionRequested;

	/// <summary>
	/// Returns the splash title text shown in the 3D viewport.
	/// </summary>
	protected override string GetTitleText()
	{
		return "Zero Day Orbit";
	}
	
	/// <summary>
	/// Returns the initial title position, below the camera frustum.
	/// </summary>
	protected override Vector3 GetTitleStartPosition()
	{
		return new Vector3(0, -20, 2); // Start below viewport
	}
	
	/// <summary>
	/// Returns the splash title resting position at screen center.
	/// </summary>
	protected override Vector3 GetTitleEndPosition()
	{
		return new Vector3(0, 0, 2); // Earth's center
	}
	
	/// <summary>
	/// Starts the delayed transition to the configured next scene.
	/// </summary>
	protected override void OnAnimationComplete()
	{
		if (_transitionRequested)
		{
			return;
		}

		_ = RequestTransitionAsync(skipDelay: false);
	}

	/// <summary>
	/// Supports quick skip to the next scene via keyboard, mouse, or gamepad input.
	/// </summary>
	/// <param name="@event">Input event to evaluate.</param>
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_allowSkip || _transitionRequested)
		{
			return;
		}

		if (@event.IsActionPressed("ui_accept") || @event is InputEventMouseButton || @event is InputEventJoypadButton)
		{
			_ = RequestTransitionAsync(skipDelay: true);
		}
	}

	/// <summary>
	/// Requests a scene transition, optionally bypassing splash hold delay.
	/// </summary>
	/// <param name="skipDelay">When true, transitions immediately.</param>
	private async Task RequestTransitionAsync(bool skipDelay)
	{
		if (_transitionRequested)
		{
			return;
		}

		_transitionRequested = true;

		if (!skipDelay && _transitionDelaySeconds > 0f)
		{
			await ToSignal(GetTree().CreateTimer(_transitionDelaySeconds), SceneTreeTimer.SignalName.Timeout);
		}

		SceneNavigator.ChangeScene(this, _nextScenePath, _deferSceneChange);
	}
}
