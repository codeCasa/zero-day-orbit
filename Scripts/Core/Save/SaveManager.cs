using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace ZeroDayOrbit.Core.Save;

/// <summary>
/// Handles persistent save file IO and slot discovery.
/// </summary>
public static class SaveManager
{
    private const string SaveDirectoryPath = "user://saves";
    private const string SaveExtension = ".json";

    /// <summary>
    /// Lists known save slots sorted by newest first.
    /// </summary>
    /// <returns>Save slot metadata list.</returns>
    public static IReadOnlyList<SaveMetadata> ListSaves()
    {
        EnsureSaveDirectory();

        var results = new List<SaveMetadata>();
        using DirAccess dir = DirAccess.Open(SaveDirectoryPath);
        if (dir == null)
        {
            GD.PushWarning($"[SaveManager] Could not open save directory {SaveDirectoryPath}");
            return results;
        }

        dir.ListDirBegin();
        while (true)
        {
            string fileName = dir.GetNext();
            if (string.IsNullOrEmpty(fileName))
            {
                break;
            }

            if (fileName == "." || fileName == ".." || fileName.StartsWith(".", StringComparison.Ordinal))
            {
                continue;
            }

            if (dir.CurrentIsDir() || !fileName.EndsWith(SaveExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string slotId = System.IO.Path.GetFileNameWithoutExtension(fileName);
            if (TryLoadFromSlot(slotId, out SaveGameData data, out _))
            {
                SaveMetadata metadata = data.Metadata ?? new SaveMetadata { SlotId = slotId };
                metadata.SlotId = string.IsNullOrWhiteSpace(metadata.SlotId) ? slotId : metadata.SlotId;
                metadata.FilePath = ToSavePath(slotId);
                results.Add(metadata);
            }
        }

        dir.ListDirEnd();
        return results.OrderByDescending(ParseSavedAtUtc).ToList();
    }

    /// <summary>
    /// Saves state to a named slot.
    /// </summary>
    /// <param name="slotId">Save slot id.</param>
    /// <param name="data">Save payload.</param>
    /// <param name="error">Error text on failure.</param>
    /// <returns>True when write succeeds.</returns>
    public static bool SaveToSlot(string slotId, SaveGameData data, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(slotId))
        {
            error = "slotId is empty";
            return false;
        }

        if (data == null)
        {
            error = "save data is null";
            return false;
        }

        EnsureSaveDirectory();
        string path = ToSavePath(slotId);

        data.Metadata ??= new SaveMetadata();
        data.Metadata.SlotId = slotId;
        data.Metadata.FilePath = path;
        data.Metadata.SavedAtUtc = DateTime.UtcNow.ToString("O");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(data, options);
        using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file == null)
        {
            error = $"Could not open save path for write: {path}";
            return false;
        }

        file.StoreString(json);
        GD.Print($"[SaveManager] Saved slot '{slotId}' to {path}");
        return true;
    }

    /// <summary>
    /// Loads state from a named slot.
    /// </summary>
    /// <param name="slotId">Save slot id.</param>
    /// <param name="data">Loaded save payload.</param>
    /// <param name="error">Error text on failure.</param>
    /// <returns>True when load succeeds.</returns>
    public static bool TryLoadFromSlot(string slotId, out SaveGameData data, out string error)
    {
        data = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(slotId))
        {
            error = "slotId is empty";
            return false;
        }

        string path = ToSavePath(slotId);
        if (!Godot.FileAccess.FileExists(path))
        {
            error = $"Save slot '{slotId}' does not exist.";
            return false;
        }

        using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            error = $"Could not open save file: {path}";
            return false;
        }

        string json = file.GetAsText();
        if (string.IsNullOrWhiteSpace(json))
        {
            error = $"Save file is empty: {path}";
            return false;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            data = JsonSerializer.Deserialize<SaveGameData>(json, options);
            if (data == null)
            {
                error = $"Save file failed to deserialize: {path}";
                return false;
            }

            data.Metadata ??= new SaveMetadata();
            data.Metadata.SlotId = string.IsNullOrWhiteSpace(data.Metadata.SlotId) ? slotId : data.Metadata.SlotId;
            data.Metadata.FilePath = path;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to deserialize save file '{slotId}': {ex.Message}";
            GD.PushWarning($"[SaveManager] {error}");
            return false;
        }
    }

    /// <summary>
    /// Deletes an existing save slot file.
    /// </summary>
    /// <param name="slotId">Save slot id.</param>
    /// <returns>True when deleted.</returns>
    public static bool DeleteSave(string slotId)
    {
        string path = ToSavePath(slotId);
        if (!Godot.FileAccess.FileExists(path))
        {
            return false;
        }

        Error result = DirAccess.RemoveAbsolute(path);
        return result == Error.Ok;
    }

    /// <summary>
    /// Generates a unique auto slot id.
    /// </summary>
    /// <returns>Slot identifier string.</returns>
    public static string CreateAutoSlotId()
    {
        return $"slot_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
    }

    private static string ToSavePath(string slotId)
    {
        string sanitized = slotId.Replace("/", "_").Replace("\\", "_").Trim();
        return $"{SaveDirectoryPath}/{sanitized}{SaveExtension}";
    }

    private static void EnsureSaveDirectory()
    {
        if (!DirAccess.DirExistsAbsolute(SaveDirectoryPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(SaveDirectoryPath);
        }
    }

    private static DateTime ParseSavedAtUtc(SaveMetadata metadata)
    {
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.SavedAtUtc))
        {
            return DateTime.MinValue;
        }

        return DateTime.TryParse(metadata.SavedAtUtc, out DateTime parsed) ? parsed : DateTime.MinValue;
    }
}
