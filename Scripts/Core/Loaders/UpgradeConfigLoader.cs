using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using ZeroDayOrbit.Core.Models;

namespace ZeroDayOrbit.Core.Loaders;

/// <summary>
/// Loads upgrade definitions from a JSON resource file.
/// </summary>
public static class UpgradeConfigLoader
{
    private sealed class UpgradeConfigRoot
    {
        public List<UpgradeData> Upgrades { get; set; } = [];
    }

    /// <summary>
    /// Attempts to load upgrade definitions from the given Godot resource path.
    /// </summary>
    /// <param name="resourcePath">Godot resource path, for example res://Data/upgrades.json.</param>
    /// <param name="upgrades">Loaded upgrade definitions when successful.</param>
    /// <param name="error">Human-readable error when load fails.</param>
    /// <returns>True when parsing succeeded; otherwise false.</returns>
    public static bool TryLoad(string resourcePath, out List<UpgradeData> upgrades, out string error)
    {
        upgrades = [];
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            error = "Upgrade config path is empty.";
            return false;
        }

        if (!FileAccess.FileExists(resourcePath))
        {
            error = $"Upgrade config not found at {resourcePath}.";
            return false;
        }

        using FileAccess file = FileAccess.Open(resourcePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            error = $"Could not open upgrade config at {resourcePath}.";
            return false;
        }

        string json = file.GetAsText();
        if (string.IsNullOrWhiteSpace(json))
        {
            error = $"Upgrade config at {resourcePath} is empty.";
            return false;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        try
        {
            UpgradeConfigRoot root = JsonSerializer.Deserialize<UpgradeConfigRoot>(json, options);
            if (root?.Upgrades is { Count: > 0 })
            {
                upgrades = root.Upgrades;
                return true;
            }

            List<UpgradeData> directList = JsonSerializer.Deserialize<List<UpgradeData>>(json, options);
            if (directList is { Count: > 0 })
            {
                upgrades = directList;
                return true;
            }

            error = $"No upgrades found in {resourcePath}.";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Failed parsing upgrade config {resourcePath}: {ex.Message}";
            GD.PrintErr(error);
            return false;
        }
    }
}
