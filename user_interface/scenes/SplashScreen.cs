using Godot;
using System;

public partial class SplashScreen : BaseTitleScreen
{
	protected override string GetTitleText()
	{
		return "Zero Day Orbit";
	}
	
	protected override Vector3 GetTitleStartPosition()
	{
		return new Vector3(0, -20, 2); // Start below viewport
	}
	
	protected override Vector3 GetTitleEndPosition()
	{
		return new Vector3(0, 0, 2); // Earth's center
	}
	
	protected override void OnAnimationComplete()
	{
		// Transition to Title Screen
		GetTree().ChangeSceneToFile("res://user_interface/scenes/title_screen.tscn");
	}
}
