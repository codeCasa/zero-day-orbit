using Godot;

namespace ZeroDayOrbit.Scenes;

/// <summary>
/// Manages the 3D space environment and synchronises visual elements with the orbit simulation.
/// Drives sun (DirectionalLight3D) rotation for day/night lighting, slow Earth rotation,
/// and ambient light energy transitions between daylight and darkness.
/// </summary>
public partial class SpaceEnvironmentController : Node3D
{
    /// <summary>Path to the DirectionalLight3D sun node, relative to this node.</summary>
    [Export]
    private NodePath _sunPath = "Sun";

    /// <summary>Path to the Earth model root node, relative to this node.</summary>
    [Export]
    private NodePath _earthPath = "Earth";

    /// <summary>Path to the WorldEnvironment node, relative to this node.</summary>
    [Export]
    private NodePath _worldEnvPath = "WorldEnvironment";

    /// <summary>Passive rotation speed of the Earth model in degrees per second.</summary>
    [Export(PropertyHint.Range, "0.5,20,0.5")]
    private float _earthRotationDegreesPerSecond = 4f;

    /// <summary>DirectionalLight energy when the station is in the sunlit arc.</summary>
    [Export(PropertyHint.Range, "0.5,5,0.1")]
    private float _dayLightEnergy = 2.0f;

    /// <summary>DirectionalLight energy when the station is in the dark arc.</summary>
    [Export(PropertyHint.Range, "0,0.5,0.01")]
    private float _nightLightEnergy = 0.04f;

    /// <summary>Ambient environment energy during daylight.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")]
    private float _dayAmbientEnergy = 0.2f;

    /// <summary>Ambient environment energy during nighttime (very dim).</summary>
    [Export(PropertyHint.Range, "0,1,0.05")]
    private float _nightAmbientEnergy = 0.03f;

    private DirectionalLight3D _sun;
    private Node3D _earth;
    private WorldEnvironment _worldEnvironment;

    /// <inheritdoc/>
    public override void _Ready()
    {
        _sun = GetNodeOrNull<DirectionalLight3D>(_sunPath);
        _earth = GetNodeOrNull<Node3D>(_earthPath);
        _worldEnvironment = GetNodeOrNull<WorldEnvironment>(_worldEnvPath);

        if (_sun == null)
        {
            GD.PushWarning("[SpaceEnvironmentController] Sun node not found at: " + _sunPath);
        }
    }

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        _earth?.RotateY(Mathf.DegToRad(_earthRotationDegreesPerSecond) * (float)delta);
    }

    /// <summary>
    /// Synchronises the visual environment with the current orbit simulation state.
    /// Rotates the sun light to match orbit progress (0 = day-start, 0.5 = night-start)
    /// and lerps ambient light energy for a smooth day/night transition.
    /// </summary>
    /// <param name="normalizedProgress">Orbit completion 0–1. First half is daytime.</param>
    /// <param name="isDaytime">Whether the station is in the sunlit half of the orbit.</param>
    public void SyncOrbit(float normalizedProgress, bool isDaytime)
    {
        if (_sun != null)
        {
            // -90° puts light pointing upward at orbit start (station in sunlight).
            // Rotate a full 360° over the orbit so the light swings behind at night.
            float angleDeg = -90f + normalizedProgress * 360f;
            _sun.RotationDegrees = new Vector3(angleDeg, 10f, 0f);

            // Lerp energy so the scene visibly dims as the station enters shadow.
            float targetEnergy = isDaytime ? _dayLightEnergy : _nightLightEnergy;
            _sun.LightEnergy = Mathf.Lerp(_sun.LightEnergy, targetEnergy, 0.06f);
        }

        if (_worldEnvironment?.Environment != null)
        {
            Godot.Environment env = _worldEnvironment.Environment;
            float targetAmbient = isDaytime ? _dayAmbientEnergy : _nightAmbientEnergy;
            env.AmbientLightEnergy = Mathf.Lerp(env.AmbientLightEnergy, targetAmbient, 0.06f);
        }
    }
}
