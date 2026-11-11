using System;
using System.IO;

namespace Keel.Sim.Persistence;

public static class WorldSaveStore
{
    public static (bool IsSuccess, string? ErrorMessage) SaveToFile(string path, WorldSaveData data)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, "Save path cannot be null or empty.");
        }

        string tempPath = path + ".tmp";
        try
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = WorldSaveSerializer.Serialize(data);
            File.WriteAllText(tempPath, json);

            File.Move(tempPath, path, overwrite: true);
            return (true, null);
        }
        catch (Exception ex)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Suppress secondary cleanup exceptions
            }

            return (false, $"Failed to save world state: {ex.Message}");
        }
    }

    public static WorldSaveResult LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new WorldSaveResult(null, false, "Save file not found.");
        }

        try
        {
            string json = File.ReadAllText(path);
            return WorldSaveSerializer.Deserialize(json);
        }
        catch (Exception ex)
        {
            return new WorldSaveResult(null, false, $"Failed to read save file: {ex.Message}");
        }
    }
}
