using Godot;
using ZeroDayOrbit.Core.Models;
using ZeroDayOrbit.Core.Save;

namespace ZeroDayOrbit.Core.Managers;

/// <summary>
/// Tracks orbital phase and coarse station orbit health metrics.
/// </summary>
public sealed class OrbitManager
{
    /// <summary>
    /// Gets orbit period duration in seconds.
    /// </summary>
    public float OrbitDurationSeconds { get; private set; }

    /// <summary>
    /// Gets elapsed time within the current orbit cycle.
    /// </summary>
    public float ElapsedOrbitTime { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the station is currently in daylight.
    /// </summary>
    public bool IsDaytime { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether the station is currently in darkness.
    /// </summary>
    public bool IsNighttime => !IsDaytime;

    /// <summary>
    /// Gets normalized orbit progress in range 0-1.
    /// </summary>
    public float NormalizedOrbitProgress => OrbitDurationSeconds <= 0f ? 0f : ElapsedOrbitTime / OrbitDurationSeconds;

    /// <summary>
    /// Gets current orbit stability as a normalized value from 0 to 100.
    /// </summary>
    public float OrbitStability { get; private set; } = 100f;

    /// <summary>
    /// Initializes a new orbit manager with a configurable cycle duration.
    /// </summary>
    /// <param name="orbitDurationSeconds">Desired orbit duration in seconds.</param>
    public OrbitManager(float orbitDurationSeconds = 180f)
    {
        OrbitDurationSeconds = Mathf.Max(30f, orbitDurationSeconds);
    }

    /// <summary>
    /// Changes the orbit cycle duration.
    /// </summary>
    /// <param name="orbitDurationSeconds">New duration in seconds.</param>
    public void SetOrbitDuration(float orbitDurationSeconds)
    {
        OrbitDurationSeconds = Mathf.Max(30f, orbitDurationSeconds);
        ElapsedOrbitTime = Mathf.Clamp(ElapsedOrbitTime, 0f, OrbitDurationSeconds);
    }

    /// <summary>
    /// Advances orbital state and updates derived day/night phase.
    /// </summary>
    /// <param name="delta">Elapsed simulation time in seconds.</param>
    public void Update(float delta)
    {
        ElapsedOrbitTime += delta;

        if (ElapsedOrbitTime >= OrbitDurationSeconds)
        {
            ElapsedOrbitTime -= OrbitDurationSeconds;
        }

        float phase = NormalizedOrbitProgress;
        IsDaytime = phase < 0.5f;

        if (OrbitStability > 0f)
        {
            OrbitStability = Mathf.Max(0f, OrbitStability - (0.01f * delta));
        }
    }

    /// <summary>
    /// Applies a stability delta to the orbit state.
    /// </summary>
    /// <param name="delta">Signed stability change to apply.</param>
    public void ModifyOrbitStability(float delta)
    {
        OrbitStability = Mathf.Clamp(OrbitStability + delta, 0f, 100f);
    }

    /// <summary>
    /// Builds a lightweight orbit state snapshot for UI/debug use.
    /// </summary>
    /// <returns>Current orbit state data.</returns>
    public OrbitStatusSnapshot CreateSnapshot()
    {
        return new OrbitStatusSnapshot
        {
            ElapsedOrbitTime = ElapsedOrbitTime,
            NormalizedOrbitProgress = NormalizedOrbitProgress,
            IsDaytime = IsDaytime,
            IsNighttime = IsNighttime,
            OrbitStability = OrbitStability
        };
    }

    /// <summary>
    /// Gets the latest orbit snapshot.
    /// </summary>
    /// <returns>Current orbit state data.</returns>
    public OrbitStatusSnapshot GetSnapshot()
    {
        return CreateSnapshot();
    }

    /// <summary>
    /// Creates serializable orbit save data.
    /// </summary>
    public OrbitSaveData CreateSaveData()
    {
        return new OrbitSaveData
        {
            OrbitDurationSeconds = OrbitDurationSeconds,
            ElapsedOrbitTime = ElapsedOrbitTime,
            IsDaytime = IsDaytime,
            OrbitStability = OrbitStability
        };
    }

    /// <summary>
    /// Restores orbit state from serialized save data.
    /// </summary>
    public void LoadFromSaveData(OrbitSaveData data)
    {
        if (data == null)
        {
            return;
        }

        OrbitDurationSeconds = Mathf.Max(30f, data.OrbitDurationSeconds);
        ElapsedOrbitTime = Mathf.Clamp(data.ElapsedOrbitTime, 0f, OrbitDurationSeconds);
        IsDaytime = data.IsDaytime;
        OrbitStability = Mathf.Clamp(data.OrbitStability, 0f, 100f);
    }
}
