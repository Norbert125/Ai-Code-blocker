using System.Text.Json;

namespace AiCodingBlocker.Core.State;

/// <summary>
/// Persists whether Focus Mode is ON in state.json. The service writes it, the UI reads it.
/// </summary>
public sealed class StateStore(string path)
{
    private sealed record StateFile(bool IsOn);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Missing file = OFF (fresh install). Unreadable file = ON, so the service reapplies.</summary>
    public bool Load()
    {
        if (!File.Exists(path))
            return false;

        try
        {
            return JsonSerializer.Deserialize<StateFile>(File.ReadAllText(path), Json)?.IsOn ?? true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>Writes to a temp file first, then swaps it in, so a crash can't leave half a file.</summary>
    public void Save(bool isOn)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new StateFile(isOn), Json));
        File.Move(temp, path, overwrite: true);
    }
}
