using System;
using System.Collections.Generic;
using ZeroDayOrbit.Core.Interfaces;

namespace ZeroDayOrbit.Core.Managers;

/// <summary>
/// Holds cosmic events and performs lightweight periodic random trigger checks.
/// </summary>
public sealed class CosmicEventManager
{
    private readonly List<ICosmicEvent> _availableEvents = new();
    private readonly Random _random = new();

    private float _timeUntilNextCheck = 30f;

    /// <summary>
    /// Gets or sets a value indicating whether random events can be triggered.
    /// </summary>
    public bool RandomEventsEnabled { get; set; }

    /// <summary>
    /// Gets all currently registered event definitions.
    /// </summary>
    public IReadOnlyList<ICosmicEvent> AvailableEvents => _availableEvents;

    /// <summary>
    /// Registers a cosmic event implementation for future random selection.
    /// </summary>
    /// <param name="cosmicEvent">Event definition to add.</param>
    public void AddEvent(ICosmicEvent cosmicEvent)
    {
        _availableEvents.Add(cosmicEvent);
    }

    /// <summary>
    /// Advances event timers and may trigger one random event.
    /// </summary>
    /// <param name="delta">Elapsed simulation time in seconds.</param>
    /// <param name="systemManager">System manager passed to triggered events.</param>
    public void Update(float delta, SystemManager systemManager)
    {
        _timeUntilNextCheck -= delta;
        if (_timeUntilNextCheck > 0f)
        {
            return;
        }

        _timeUntilNextCheck = 30f;

        if (!RandomEventsEnabled || _availableEvents.Count == 0)
        {
            return;
        }

        const double triggerChance = 0.10;
        if (_random.NextDouble() > triggerChance)
        {
            return;
        }

        int index = _random.Next(0, _availableEvents.Count);
        _availableEvents[index].Apply(systemManager);
    }
}
