namespace ZeroDayOrbit.Infrastructure;

/// <summary>
/// Lightweight cross-scene handoff for gameplay boot mode.
/// </summary>
public static class GameSessionBootstrap
{
    /// <summary>
    /// Gets pending save slot id to load when gameplay scene boots.
    /// </summary>
    public static string PendingLoadSlotId { get; private set; } = string.Empty;

    /// <summary>
    /// Sets the pending load slot id for next gameplay boot.
    /// </summary>
    /// <param name="slotId">Save slot id.</param>
    public static void SetPendingLoadSlot(string slotId)
    {
        PendingLoadSlotId = slotId ?? string.Empty;
    }

    /// <summary>
    /// Clears pending load slot and forces next gameplay boot to new game mode.
    /// </summary>
    public static void ClearPendingLoadSlot()
    {
        PendingLoadSlotId = string.Empty;
    }
}
