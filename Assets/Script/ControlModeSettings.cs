using System;
using System.IO;
using UnityEngine;

public enum ControlMode
{
    PC = 0,
    Mobile = 1
}

[Serializable]
public class ControlModeSettingsData
{
    public int Mode = (int)ControlMode.PC;
}

public static class ControlModeSettings
{
    private const string FileName = "control_mode_settings.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSavedMode()
    {
        return File.Exists(FilePath);
    }

    public static ControlMode LoadOrDefault(ControlMode defaultMode = ControlMode.PC)
    {
        try
        {
            if (!File.Exists(FilePath))
                return defaultMode;

            string json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
                return defaultMode;

            var data = JsonUtility.FromJson<ControlModeSettingsData>(json);
            if (data == null)
                return defaultMode;

            int clamped = Mathf.Clamp(data.Mode, 0, 1);
            return (ControlMode)clamped;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ControlModeSettings] Load failed: {ex.Message}");
            return defaultMode;
        }
    }

    public static void Save(ControlMode mode)
    {
        try
        {
            var data = new ControlModeSettingsData { Mode = (int)mode };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ControlModeSettings] Save failed: {ex.Message}");
        }
    }
}
