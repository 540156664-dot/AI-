using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common;

/// <summary>
/// 预设方案管理器 — 保存/加载/删除JSON格式的预设参数
/// </summary>
public class PresetManager
{
    private readonly string _presetDir;

    public PresetManager(string? presetDir = null)
    {
        _presetDir = presetDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GstarRackAddin", "Presets");
        Directory.CreateDirectory(_presetDir);
    }

    public void SavePreset<T>(string category, string presetName, T data)
    {
        var dir = Path.Combine(_presetDir, category);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{presetName}.json");
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public T? LoadPreset<T>(string category, string presetName)
    {
        var path = Path.Combine(_presetDir, category, $"{presetName}.json");
        if (!File.Exists(path)) return default;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json);
    }

    public List<string> ListPresets(string category)
    {
        var dir = Path.Combine(_presetDir, category);
        if (!Directory.Exists(dir)) return new List<string>();
        return Directory.GetFiles(dir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList()!;
    }

    public void DeletePreset(string category, string presetName)
    {
        var path = Path.Combine(_presetDir, category, $"{presetName}.json");
        if (File.Exists(path)) File.Delete(path);
    }

    public string PresetDirectory => _presetDir;
}
