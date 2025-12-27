using Godot;
using System;

public partial class TitleScreen : BaseTitleScreen
{
	protected override string GetTitleText()
	{
		return "Zero Day Orbit";
	}
	
	protected override Vector3 GetTitleStartPosition()
	{
		return new Vector3(0, 3, 2); // Above Earth
	}
	
	protected override Vector3 GetTitleEndPosition()
	{
		return new Vector3(0, 3, 2); // Stay above Earth (no animation)
	}
	
	protected override void OnAnimationComplete()
	{
		// Main menu is ready - no transition needed
		// Could add menu interaction logic here
	}
	
	protected override void InitializeScene()
	{
		base.InitializeScene();
		
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
		
		// Skip animation phases - start with everything visible
		_elapsedTime = TitleSlideEnd + 1.0f; // Jump past all animations
		_starsMaterial.SetShaderParameter("fade_alpha", 1.0f);
		_3dViewRect.Modulate = new Color(1, 1, 1, 1);
		_titleMesh.Position = GetTitleEndPosition();
		_animationCompleted = true;
	}
}
