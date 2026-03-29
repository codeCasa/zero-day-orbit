namespace ZeroDayOrbit.Core.Models;

/// <summary>
/// Classifies the gameplay impact of a module failure.
/// </summary>
public enum SystemCriticality
{
    /// <summary>
    /// Failure can directly end the run.
    /// </summary>
    Critical,

    /// <summary>
    /// Failure applies severe penalties but is not an immediate loss condition.
    /// </summary>
    Important,

    /// <summary>
    /// Failure applies minor penalties or utility loss.
    /// </summary>
    Optional
}
