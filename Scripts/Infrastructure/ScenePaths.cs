namespace ZeroDayOrbit.Infrastructure;

/// <summary>
/// Centralized scene path constants used by flow/navigation scripts.
/// </summary>
public static class ScenePaths
{
    /// <summary>
    /// Splash/intro scene shown at startup.
    /// </summary>
    public const string Splash = "res://user_interface/scenes/splash_screen.tscn";

    /// <summary>
    /// Main menu scene where the player starts a run.
    /// </summary>
    public const string MainMenu = "res://user_interface/scenes/title_screen.tscn";

    /// <summary>
    /// Primary gameplay scene that hosts <c>GameRoot</c>.
    /// </summary>
    public const string Gameplay = "res://user_interface/scenes/gameplay.tscn";
}
