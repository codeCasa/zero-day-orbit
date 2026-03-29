using Godot;

namespace ZeroDayOrbit.Infrastructure;

/// <summary>
/// Lightweight helper for scene changes with optional deferred execution.
/// </summary>
public static class SceneNavigator
{
    /// <summary>
    /// Requests a scene change using a node context.
    /// </summary>
    /// <param name="contextNode">Node used to resolve the active <see cref="SceneTree"/>.</param>
    /// <param name="scenePath">Target scene path.</param>
    /// <param name="deferred">When true, queues the scene change through <see cref="Object.CallDeferred"/>.</param>
    /// <returns>True when a scene change request was issued; otherwise false.</returns>
    public static bool ChangeScene(Node contextNode, string scenePath, bool deferred = true)
    {
        if (contextNode == null)
        {
            GD.PushError("SceneNavigator.ChangeScene failed: contextNode is null.");
            return false;
        }

        return ChangeScene(contextNode.GetTree(), scenePath, deferred);
    }

    /// <summary>
    /// Requests a scene change using an explicit <see cref="SceneTree"/>.
    /// </summary>
    /// <param name="tree">Scene tree to operate on.</param>
    /// <param name="scenePath">Target scene path.</param>
    /// <param name="deferred">When true, queues the scene change through <see cref="Object.CallDeferred"/>.</param>
    /// <returns>True when a scene change request was issued; otherwise false.</returns>
    public static bool ChangeScene(SceneTree tree, string scenePath, bool deferred = true)
    {
        if (tree == null)
        {
            GD.PushError("SceneNavigator.ChangeScene failed: tree is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(scenePath))
        {
            GD.PushError("SceneNavigator.ChangeScene failed: scenePath is empty.");
            return false;
        }

        if (!ResourceLoader.Exists(scenePath))
        {
            GD.PushError($"SceneNavigator.ChangeScene failed: scene does not exist at '{scenePath}'.");
            return false;
        }

        if (deferred)
        {
            tree.CallDeferred(SceneTree.MethodName.ChangeSceneToFile, scenePath);
            return true;
        }

        Error changeError = tree.ChangeSceneToFile(scenePath);
        if (changeError != Error.Ok)
        {
            GD.PushError($"SceneNavigator.ChangeScene failed for '{scenePath}' with error: {changeError}");
            return false;
        }

        return true;
    }
}
