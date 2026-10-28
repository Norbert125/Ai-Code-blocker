namespace AiCodingBlocker.Core;

/// <summary>Fixed locations shared by the service and the UI.</summary>
public static class Paths
{
    public const string PipeName = "AiCodingBlocker";

    public static readonly string DataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AiCodingBlocker");

    public static readonly string ConfigPath = Path.Combine(DataDir, "config.json");
    public static readonly string StatePath = Path.Combine(DataDir, "state.json");
    public static readonly string BackupDir = Path.Combine(DataDir, "backups");
    public static readonly string HostsPath = Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts");
}
