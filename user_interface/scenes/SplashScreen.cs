using Godot;
using System;

public partial class SplashScreen : Control
{
	private TextureRect _starsRect;
	private ShaderMaterial _starsMaterial;
	private float _elapsedTime = 0.0f;
	
	private SubViewport _subViewport;
	private Camera3D _camera;
	private Node3D _stationNode;
	private Node3D _earthNode;
	private Node3D _sunNode;
	private TextureRect _3dViewRect;
	private DirectionalLight3D _light;
	private bool _stationVisible = false;
	
	// Timing constants
	private const float BlackDuration = 0.5f;
	private const float StarsFadeStart = 0.5f;
	private const float StarsFadeEnd = 2.0f;
	private const float StationFadeStart = 2.0f;
	private const float StationFadeEnd = 4.0f;
	
	// Station animation
	private Vector3 _stationStartPos;
	private Vector3 _stationEndPos;
	
	public override void _Ready()
	{
		GD.Print("SplashScreen _Ready called");
		
		// Get reference to stars TextureRect
		_starsRect = GetNode<TextureRect>("TextureRect_Stars");
		_starsMaterial = (ShaderMaterial)_starsRect.Material;
		
		// Create a simple white texture for the shader to render on
		var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
		image.Fill(Colors.White);
		var texture = ImageTexture.CreateFromImage(image);
		_starsRect.Texture = texture;
		
		// Start with stars invisible
		_starsMaterial.SetShaderParameter("fade_alpha", 0.0f);
		
		// Setup 3D viewport and station
		_subViewport = GetNode<SubViewport>("SubViewport");
		_camera = GetNode<Camera3D>("SubViewport/World3D/Camera3D");
		_stationNode = GetNode<Node3D>("SubViewport/World3D/StationNode3D");
		_earthNode = GetNode<Node3D>("SubViewport/World3D/EarthNode3D");
		_sunNode = GetNode<Node3D>("SubViewport/World3D/SunNode3D");
		_3dViewRect = GetNode<TextureRect>("TextureRect_3DView");
		_light = GetNode<DirectionalLight3D>("SubViewport/World3D/DirectionalLight3D");
		
		// Make 3D view initially invisible (will fade in later)
		_3dViewRect.Modulate = new Color(1, 1, 1, 0);
		
		// Improve lighting - make it brighter and position it better
		_light.LightEnergy = 3.0f;
		_light.Rotation = new Vector3(Mathf.DegToRad(-30), Mathf.DegToRad(30), 0);
		
		// Add environment for better visibility
		var environment = new Godot.Environment();
		environment.BackgroundMode = Godot.Environment.BGMode.Color;
		environment.BackgroundColor = Colors.Black;
		environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
		environment.AmbientLightColor = Colors.White;
		environment.AmbientLightEnergy = 0.3f;
		
		var worldEnv = new WorldEnvironment();
		worldEnv.Environment = environment;
		_subViewport.GetNode<Node3D>("World3D").AddChild(worldEnv);
		
		GD.Print($"3D View rect modulate: {_3dViewRect.Modulate}");
		
		// Calculate Sun's bounding box and position it in top-left corner
		Aabb sunBounds = CalculateBounds(_sunNode);
		GD.Print($"Sun bounds: {sunBounds}");
		
		Vector3 sunSize = sunBounds.Size;
		float sunMaxDimension = Mathf.Max(sunSize.X, Mathf.Max(sunSize.Y, sunSize.Z));
		
		// Scale Sun to be visible but not dominating (background element)
		float sunTargetSize = 1.5f;
		float sunScaleMultiplier = sunTargetSize / sunMaxDimension;
		_sunNode.Scale = Vector3.One * sunScaleMultiplier;
		
		// Position Sun peeking from top-left corner
		// We need to calculate where the edge of the viewport is in world space
		Vector3 sunCenter = sunBounds.GetCenter();
		
		// Position camera first to calculate viewport bounds
		float viewHeight = 1.0f; // Temporary, will recalculate after Earth scaling
		float fovRadians = Mathf.DegToRad(_camera.Fov);
		float tempCameraDistance = (viewHeight / 2.0f) / Mathf.Tan(fovRadians / 2.0f);
		_camera.Position = new Vector3(0, 0, tempCameraDistance);
		
		// Calculate viewport dimensions at Z=0 plane (where Earth is centered)
		float viewportAspect = (float)_subViewport.Size.X / _subViewport.Size.Y;
		float viewportHeight = 2.0f * tempCameraDistance * Mathf.Tan(fovRadians / 2.0f);
		float viewportWidth = viewportHeight * viewportAspect;
		
		// Position Sun so only 4th quadrant (bottom-right) is visible in top-left corner
		// This means the Sun center is outside the viewport to the top-left
		float sunRadius = sunTargetSize / 2.0f;
		_sunNode.Position = new Vector3(
			-viewportWidth / 2.0f - sunRadius * 0.15f,  // Left edge, peeking in
			viewportHeight / 2.0f + sunRadius * 0.15f,   // Top edge, peeking down
			-6.0f  // Behind Earth
		) + (-sunCenter * sunScaleMultiplier);
		
		GD.Print($"Sun scaled by {sunScaleMultiplier}, positioned at {_sunNode.Position}");
		
		// Calculate Earth's bounding box
		Aabb earthBounds = CalculateBounds(_earthNode);
		GD.Print($"Earth bounds: {earthBounds}");
		
		Vector3 earthSize = earthBounds.Size;
		float earthMaxDimension = Mathf.Max(earthSize.X, Mathf.Max(earthSize.Y, earthSize.Z));
		
		// Scale Earth to be prominent - it's the main focus
		// Make it fill about 50% of the viewport height
		float earthTargetSize = 3.5f;
		float earthScaleMultiplier = earthTargetSize / earthMaxDimension;
		_earthNode.Scale = Vector3.One * earthScaleMultiplier;
		
		// Center Earth at origin
		Vector3 earthCenter = earthBounds.GetCenter();
		_earthNode.Position = -earthCenter * earthScaleMultiplier;
		
		GD.Print($"Earth scaled by {earthScaleMultiplier} (proportional to Sun)");
		
		// Calculate the station's bounding box
		Aabb stationBounds = CalculateBounds(_stationNode);
		GD.Print($"Station bounds: {stationBounds}");
		
		// Get the largest dimension of the station
		Vector3 size = stationBounds.Size;
		float maxDimension = Mathf.Max(size.X, Mathf.Max(size.Y, size.Z));
		
		GD.Print($"Max dimension: {maxDimension}");
		
		// Scale station to be noticeable but smaller than Earth
		// Camera is looking from behind the station, so it should be prominent
		// Make it about 1/8 the size of Earth for good visibility
		float stationScaleMultiplier = (earthTargetSize / 8.0f) * (1.0f / maxDimension);
		_stationNode.Scale = Vector3.One * stationScaleMultiplier;
		
		// Position station closer to camera (we're looking from behind it)
		// Station should be in the foreground to the left of Earth
		Vector3 stationCenter = stationBounds.GetCenter();
		float earthRadius = earthTargetSize / 2.0f;
		
		// Position station in front of Earth (positive Z = toward camera)
		// To the left side, creating a nice composition
		Vector3 orbitPosition = new Vector3(
			-earthRadius * 1.3f,  // Further left to avoid collision
			earthRadius * 0.05f,   // Slightly elevated
			earthRadius * 0.8f     // Close to camera (in front)
		);
		_stationNode.Position = orbitPosition + (-stationCenter * stationScaleMultiplier);
		
		// Store final position and calculate start position for parallax effect
		_stationEndPos = _stationNode.Position;
		_stationStartPos = _stationEndPos + new Vector3(-0.3f, 0, 0.5f); // Start from further left and closer
		_stationNode.Position = _stationStartPos;
		
		// Recalculate camera position to frame Earth nicely
		viewHeight = earthTargetSize * 1.5f;
		fovRadians = Mathf.DegToRad(_camera.Fov);
		float cameraDistance = (viewHeight / 2.0f) / Mathf.Tan(fovRadians / 2.0f);
		
		// Add rotation to the station for better visibility
		_stationNode.RotationDegrees = new Vector3(-15, 25, 0);
		
		// Position camera to look at Earth center
		_camera.Position = new Vector3(0, 0, cameraDistance);
		_camera.LookAt(Vector3.Zero, Vector3.Up);
		
		GD.Print($"Station scaled by {stationScaleMultiplier}, Camera at: {_camera.Position}");
		GD.Print($"Station position: {_stationNode.Position}, scale: {_stationNode.Scale}");
		
		// Check if station has any mesh children
		int meshCount = 0;
		CountMeshes(_stationNode, ref meshCount);
		GD.Print($"Station contains {meshCount} mesh instances");
		
		// Force viewport to update
		_subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		
		// Debug: Print station children structure
		GD.Print("Station node structure:");
		PrintNodeStructure(_stationNode, 0);
		
		GD.Print("Stars material initialized with texture");
	}
	
	// Helper method to calculate bounds of a 3D node and its children
	private Aabb CalculateBounds(Node3D node)
	{
		Aabb bounds = new Aabb();
		bool first = true;
		
		CalculateBoundsRecursive(node, ref bounds, ref first, Transform3D.Identity);
		
		return bounds;
	}
	
	private void CalculateBoundsRecursive(Node node, ref Aabb bounds, ref bool first, Transform3D parentTransform)
	{
		if (node is MeshInstance3D meshInstance)
		{
			var mesh = meshInstance.Mesh;
			if (mesh != null)
			{
				Aabb meshAabb = mesh.GetAabb();
				Transform3D globalTransform = parentTransform * meshInstance.Transform;
				
				// Transform AABB corners to global space
				Vector3[] corners = new Vector3[8];
				corners[0] = globalTransform * (meshAabb.Position);
				corners[1] = globalTransform * (meshAabb.Position + new Vector3(meshAabb.Size.X, 0, 0));
				corners[2] = globalTransform * (meshAabb.Position + new Vector3(0, meshAabb.Size.Y, 0));
				corners[3] = globalTransform * (meshAabb.Position + new Vector3(0, 0, meshAabb.Size.Z));
				corners[4] = globalTransform * (meshAabb.Position + new Vector3(meshAabb.Size.X, meshAabb.Size.Y, 0));
				corners[5] = globalTransform * (meshAabb.Position + new Vector3(meshAabb.Size.X, 0, meshAabb.Size.Z));
				corners[6] = globalTransform * (meshAabb.Position + new Vector3(0, meshAabb.Size.Y, meshAabb.Size.Z));
				corners[7] = globalTransform * (meshAabb.Position + meshAabb.Size);
				
				foreach (var corner in corners)
				{
					if (first)
					{
						bounds = new Aabb(corner, Vector3.Zero);
						first = false;
					}
					else
					{
						bounds = bounds.Expand(corner);
					}
				}
			}
		}
		
		if (node is Node3D node3D)
		{
			Transform3D newTransform = parentTransform * node3D.Transform;
			foreach (Node child in node.GetChildren())
			{
				CalculateBoundsRecursive(child, ref bounds, ref first, newTransform);
			}
		}
		else
		{
			foreach (Node child in node.GetChildren())
			{
				CalculateBoundsRecursive(child, ref bounds, ref first, parentTransform);
			}
		}
	}
	
	private void CountMeshes(Node node, ref int count)
	{
		if (node is MeshInstance3D)
		{
			count++;
		}
		
		foreach (Node child in node.GetChildren())
		{
			CountMeshes(child, ref count);
		}
	}
	
	private void PrintNodeStructure(Node node, int depth)
	{
		string indent = new string(' ', depth * 2);
		string nodeInfo = $"{indent}- {node.Name} ({node.GetType().Name})";
		
		if (node is MeshInstance3D meshInst)
		{
			nodeInfo += $" [Mesh: {meshInst.Mesh != null}, Visible: {meshInst.Visible}]";
		}
		else if (node is Node3D node3d)
		{
			nodeInfo += $" [Visible: {node3d.Visible}]";
		}
		
		GD.Print(nodeInfo);
		
		if (depth < 3) // Limit depth to avoid too much output
		{
			foreach (Node child in node.GetChildren())
			{
				PrintNodeStructure(child, depth + 1);
			}
		}
	}

	public override void _Process(double delta)
	{
		_elapsedTime += (float)delta;
		
		// Phase 1: 0-0.5s - Pure black (stars alpha = 0)
		if (_elapsedTime < BlackDuration)
		{
			_starsMaterial.SetShaderParameter("fade_alpha", 0.0f);
			_3dViewRect.Modulate = new Color(1, 1, 1, 0);
		}
		// Phase 2: 0.5-2s - Stars fade in with twinkle
		else if (_elapsedTime < StarsFadeEnd)
		{
			float fadeProgress = (_elapsedTime - StarsFadeStart) / (StarsFadeEnd - StarsFadeStart);
			float alpha = Mathf.Clamp(fadeProgress, 0.0f, 1.0f);
			_starsMaterial.SetShaderParameter("fade_alpha", alpha);
			_3dViewRect.Modulate = new Color(1, 1, 1, 0);
		}
		// Phase 3: 2-4s - Station fades in + slides forward (parallax)
		else if (_elapsedTime < StationFadeEnd)
		{
			_starsMaterial.SetShaderParameter("fade_alpha", 1.0f);
			
			if (!_stationVisible)
			{
				_stationVisible = true;
				GD.Print($"Phase 3: Station fade-in started - Time: {_elapsedTime:F2}s");
			}
			
			// Calculate fade and slide progress
			float fadeProgress = (_elapsedTime - StationFadeStart) / (StationFadeEnd - StationFadeStart);
			float smoothProgress = SmoothStep(0.0f, 1.0f, fadeProgress);
			
			// Fade in the 3D view
			_3dViewRect.Modulate = new Color(1, 1, 1, smoothProgress);
			
			// Parallax slide: station moves from back to front
			_stationNode.Position = _stationStartPos.Lerp(_stationEndPos, smoothProgress);
		}
		// Phase 4: 4s+ - Hold and wait for transition
		else
		{
			_starsMaterial.SetShaderParameter("fade_alpha", 1.0f);
			_3dViewRect.Modulate = new Color(1, 1, 1, 1);
			_stationNode.Position = _stationEndPos;
			
			// TODO: Transition to Main Menu
		}
	}
	
	// Smooth step interpolation for easing
	private float SmoothStep(float from, float to, float t)
	{
		t = Mathf.Clamp(t, 0.0f, 1.0f);
		t = t * t * (3.0f - 2.0f * t); // Smoothstep formula
		return Mathf.Lerp(from, to, t);
	}
}
