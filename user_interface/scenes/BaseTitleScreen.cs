using Godot;
using System;

public abstract partial class BaseTitleScreen : Control
{
	protected TextureRect _starsRect;
	protected ShaderMaterial _starsMaterial;
	protected float _elapsedTime = 0.0f;
	
	protected SubViewport _subViewport;
	protected Camera3D _camera;
	protected Node3D _stationNode;
	protected Node3D _earthNode;
	protected TextureRect _3dViewRect;
	protected DirectionalLight3D _light;
	protected MeshInstance3D _titleMesh;
	protected bool _stationVisible = false;
	protected bool _animationCompleted = false;
	
	// Timing constants
	protected const float BlackDuration = 0.5f;
	protected const float StarsFadeStart = 0.5f;
	protected const float StarsFadeEnd = 2.0f;
	protected const float StationFadeStart = 2.0f;
	protected const float StationFadeEnd = 4.0f;
	protected const float TitleSlideStart = 4.5f;
	protected const float TitleSlideEnd = 6.0f;
	
	// Station animation
	protected Vector3 _stationStartPos;
	protected Vector3 _stationEndPos;
	
	// Rotation and orbit animation
	protected float _earthRotationSpeed = 6.0f;
	protected float _stationOrbitSpeed = 4.0f;
	protected float _stationOrbitRadius;
	protected float _stationOrbitAngle = 0.0f;
	protected float _stationOrbitHeight;
	
	// Abstract methods for derived classes to implement
	protected abstract string GetTitleText();
	protected abstract Vector3 GetTitleStartPosition();
	protected abstract Vector3 GetTitleEndPosition();
	protected abstract void OnAnimationComplete();
	
	public override void _Ready()
	{
		InitializeScene();
	}
	
	protected virtual void InitializeScene()
	{
		GD.Print($"{GetType().Name} _Ready called");
		
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
		_3dViewRect = GetNode<TextureRect>("TextureRect_3DView");
		_light = GetNode<DirectionalLight3D>("SubViewport/World3D/DirectionalLight3D");
		
		// Create 3D text mesh for title
		_titleMesh = new MeshInstance3D();
		var textMesh = new TextMesh();
		textMesh.Text = GetTitleText();
		textMesh.HorizontalAlignment = HorizontalAlignment.Center;
		
		// Load Orbitron font
		var orbitronFont = GD.Load<FontFile>("res://fonts/Orbitron-VariableFont_wght.ttf");
		textMesh.Font = orbitronFont;
		textMesh.FontSize = 96;
		textMesh.Depth = 0.2f;
		
		// Create material for the text
		var textMaterial = new StandardMaterial3D();
		textMaterial.AlbedoColor = Colors.White;
		textMaterial.EmissionEnabled = true;
		textMaterial.EmissionEnergyMultiplier = 0.5f;
		textMaterial.Emission = new Color(0.9f, 0.9f, 1.0f);
		textMaterial.Metallic = 0.3f;
		textMaterial.Roughness = 0.4f;
		
		_titleMesh.Mesh = textMesh;
		_titleMesh.MaterialOverride = textMaterial;
		
		// Position text in 3D space
		_titleMesh.Position = GetTitleStartPosition();
		_titleMesh.Scale = Vector3.One * 0.15f;
		
		// Add to 3D world
		_subViewport.GetNode<Node3D>("World3D").AddChild(_titleMesh);
		
		// Enable shadow casting for title
		_titleMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
		
		// Make 3D view initially invisible (will fade in later)
		_3dViewRect.Modulate = new Color(1, 1, 1, 0);
		
		// Add environment for better visibility
		var environment = new Godot.Environment();
		environment.BackgroundMode = Godot.Environment.BGMode.Color;
		environment.BackgroundColor = Colors.Black;
		environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
		environment.AmbientLightColor = Colors.White;
		environment.AmbientLightEnergy = 0.15f;
		
		var worldEnv = new WorldEnvironment();
		worldEnv.Environment = environment;
		_subViewport.GetNode<Node3D>("World3D").AddChild(worldEnv);
		
		// Position DirectionalLight to represent the Sun
		_light.LightColor = new Color(1.0f, 0.85f, 0.6f);
		_light.LightEnergy = 3.0f;
		_light.Position = new Vector3(-5, 4, 2);
		_light.Rotation = new Vector3(Mathf.DegToRad(-35), Mathf.DegToRad(-25), 0);
		_light.ShadowEnabled = true;
		_light.ShadowBias = 0.1f;
		
		// Setup Earth
		SetupEarth();
		
		// Setup Station
		SetupStation();
		
		// Force viewport to update
		_subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		
		GD.Print("Stars material initialized with texture");
	}
	
	protected void SetupEarth()
	{
		Aabb earthBounds = CalculateBounds(_earthNode);
		Vector3 earthSize = earthBounds.Size;
		float earthMaxDimension = Mathf.Max(earthSize.X, Mathf.Max(earthSize.Y, earthSize.Z));
		
		float earthTargetSize = 3.5f;
		float earthScaleMultiplier = earthTargetSize / earthMaxDimension;
		_earthNode.Scale = Vector3.One * earthScaleMultiplier;
		
		Vector3 earthCenter = earthBounds.GetCenter();
		_earthNode.Position = -earthCenter * earthScaleMultiplier;
		
		EnableShadows(_earthNode);
		
		// Position camera
		var viewHeight = earthTargetSize * 1.5f;
		var fovRadians = Mathf.DegToRad(_camera.Fov);
		float cameraDistance = (viewHeight / 2.0f) / Mathf.Tan(fovRadians / 2.0f);
		_camera.Position = new Vector3(0, 0, cameraDistance);
		_camera.LookAt(Vector3.Zero, Vector3.Up);
	}
	
	protected void SetupStation()
	{
		Aabb stationBounds = CalculateBounds(_stationNode);
		Vector3 size = stationBounds.Size;
		float maxDimension = Mathf.Max(size.X, Mathf.Max(size.Y, size.Z));
		
		float earthTargetSize = 3.5f;
		float stationScaleMultiplier = (earthTargetSize / 8.0f) * (1.0f / maxDimension);
		_stationNode.Scale = Vector3.One * stationScaleMultiplier;
		
		Vector3 stationCenter = stationBounds.GetCenter();
		float earthRadius = earthTargetSize / 2.0f;
		
		Vector3 orbitPosition = new Vector3(
			-earthRadius * 1.3f,
			earthRadius * 0.05f,
			earthRadius * 0.8f
		);
		_stationNode.Position = orbitPosition + (-stationCenter * stationScaleMultiplier);
		
		_stationOrbitRadius = earthRadius * 1.3f;
		_stationOrbitHeight = earthRadius * 0.05f;
		_stationOrbitAngle = Mathf.Atan2(orbitPosition.X, orbitPosition.Z);
		
		EnableShadows(_stationNode);
		
		_stationEndPos = _stationNode.Position;
		_stationStartPos = _stationEndPos + new Vector3(-0.3f, 0, 0.5f);
		_stationNode.Position = _stationStartPos;
		
		_stationNode.RotationDegrees = new Vector3(-15, 25, 0);
	}
	
	public override void _Process(double delta)
	{
		_elapsedTime += (float)delta;
		
		// Continuously rotate Earth
		_earthNode.RotateY(Mathf.DegToRad(_earthRotationSpeed * (float)delta));
		
		// Orbit station around Earth after it's visible
		if (_elapsedTime >= StationFadeStart)
		{
			_stationOrbitAngle += Mathf.DegToRad(_stationOrbitSpeed * (float)delta);
			
			Vector3 orbitPos = new Vector3(
				Mathf.Sin(_stationOrbitAngle) * _stationOrbitRadius,
				_stationOrbitHeight,
				Mathf.Cos(_stationOrbitAngle) * _stationOrbitRadius
			);
			
			if (_elapsedTime < StationFadeEnd)
			{
				float fadeProgress = (_elapsedTime - StationFadeStart) / (StationFadeEnd - StationFadeStart);
				float smoothProgress = SmoothStep(0.0f, 1.0f, fadeProgress);
				_stationNode.Position = _stationStartPos.Lerp(orbitPos, smoothProgress);
			}
			else
			{
				_stationNode.Position = orbitPos;
			}
			
			_stationEndPos = orbitPos;
		}
		
		// Phase 1: 0-0.5s - Pure black
		if (_elapsedTime < BlackDuration)
		{
			_starsMaterial.SetShaderParameter("fade_alpha", 0.0f);
			_3dViewRect.Modulate = new Color(1, 1, 1, 0);
		}
		// Phase 2: 0.5-2s - Stars fade in
		else if (_elapsedTime < StarsFadeEnd)
		{
			float fadeProgress = (_elapsedTime - StarsFadeStart) / (StarsFadeEnd - StarsFadeStart);
			float alpha = Mathf.Clamp(fadeProgress, 0.0f, 1.0f);
			_starsMaterial.SetShaderParameter("fade_alpha", alpha);
			_3dViewRect.Modulate = new Color(1, 1, 1, 0);
		}
		// Phase 3: 2-4s - Station fades in
		else if (_elapsedTime < StationFadeEnd)
		{
			_starsMaterial.SetShaderParameter("fade_alpha", 1.0f);
			
			if (!_stationVisible)
			{
				_stationVisible = true;
				GD.Print($"Phase 3: Station fade-in started - Time: {_elapsedTime:F2}s");
			}
			
			float fadeProgress = (_elapsedTime - StationFadeStart) / (StationFadeEnd - StationFadeStart);
			float smoothProgress = SmoothStep(0.0f, 1.0f, fadeProgress);
			_3dViewRect.Modulate = new Color(1, 1, 1, smoothProgress);
		}
		// Phase 4: 4s+ - Hold and continue orbit
		else
		{
			_starsMaterial.SetShaderParameter("fade_alpha", 1.0f);
			_3dViewRect.Modulate = new Color(1, 1, 1, 1);
		}
		
		// Title animation
		if (_elapsedTime >= TitleSlideStart && _elapsedTime < TitleSlideEnd)
		{
			float slideProgress = (_elapsedTime - TitleSlideStart) / (TitleSlideEnd - TitleSlideStart);
			float smoothProgress = ApplyTitleSlideEasing(slideProgress);
			
			Vector3 startPos = GetTitleStartPosition();
			Vector3 endPos = GetTitleEndPosition();
			_titleMesh.Position = startPos.Lerp(endPos, smoothProgress);
		}
		else if (_elapsedTime >= TitleSlideEnd)
		{
			_titleMesh.Position = GetTitleEndPosition();
			if (!_animationCompleted)
			{
				_animationCompleted = true;
				OnAnimationComplete();
			}
		}
	}
	
	// Helper methods
	protected Aabb CalculateBounds(Node3D node)
	{
		Aabb bounds = new Aabb();
		bool first = true;
		CalculateBoundsRecursive(node, ref bounds, ref first, Transform3D.Identity);
		return bounds;
	}
	
	protected void CalculateBoundsRecursive(Node node, ref Aabb bounds, ref bool first, Transform3D parentTransform)
	{
		if (node is MeshInstance3D meshInstance)
		{
			var mesh = meshInstance.Mesh;
			if (mesh != null)
			{
				Aabb meshAabb = mesh.GetAabb();
				Transform3D globalTransform = parentTransform * meshInstance.Transform;
				
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
	
	protected void EnableShadows(Node node)
	{
		if (node is MeshInstance3D meshInst)
		{
			meshInst.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
		}
		
		foreach (Node child in node.GetChildren())
		{
			EnableShadows(child);
		}
	}
	
	protected float SmoothStep(float from, float to, float t)
	{
		t = Mathf.Clamp(t, 0.0f, 1.0f);
		t = t * t * (3.0f - 2.0f * t);
		return Mathf.Lerp(from, to, t);
	}

	protected virtual float ApplyTitleSlideEasing(float t)
	{
		return SmoothStep(0.0f, 1.0f, t);
	}
}
