namespace AiCodingBlocker.Core.Blockers;

/// <summary>
/// Blocks AI domains through a marked section at the end of the hosts file.
/// Lines outside the markers are never changed.
/// </summary>
public sealed class OnlineBlocker : IBlocker
{
    public const string StartMarker = "# >>> AiCodingBlocker";
    public const string EndMarker = "# <<< AiCodingBlocker";

    private readonly string _hostsPath;
    private readonly string _backupDir;
    private readonly IReadOnlyList<string> _domains;

    public OnlineBlocker(string hostsPath, string backupDir, IEnumerable<string> domains)
    {
        _hostsPath = hostsPath;
        _backupDir = backupDir;
        _domains = domains.ToList();
    }

    public string Name => "Online";

    public BlockerResult Apply()
    {
        try
        {
            var lines = File.ReadAllLines(_hostsPath).ToList();
            if (!TryFindBlock(lines, out int start, out int end, out string? error))
                return Fail(error);

            if (start >= 0)
            {
                // Block already there: swap in the current domain list, no duplicate.
                lines.RemoveRange(start, end - start + 1);
                lines.InsertRange(start, BuildBlock());
            }
            else
            {
                // First write since the file was clean: keep a copy of that clean file.
                Backup();
                lines.AddRange(BuildBlock());
            }

            File.WriteAllLines(_hostsPath, lines);
            return new BlockerResult(Name, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Fail(ex.Message);
        }
    }

    public BlockerResult Remove()
    {
        try
        {
            var lines = File.ReadAllLines(_hostsPath).ToList();
            if (!TryFindBlock(lines, out int start, out int end, out string? error))
                return Fail(error);

            if (start < 0)
                return new BlockerResult(Name, true); // nothing of ours to remove

            lines.RemoveRange(start, end - start + 1);
            File.WriteAllLines(_hostsPath, lines);
            return new BlockerResult(Name, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Fail(ex.Message);
        }
    }

    /// <summary>
    /// Finds our block. Returns false if the markers are broken (missing end, duplicates,
    /// wrong order); in that case nothing may be written.
    /// </summary>
    private static bool TryFindBlock(List<string> lines, out int start, out int end, out string? error)
    {
        start = end = -1;
        error = null;
        int starts = 0, ends = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i].Trim();
            if (line == StartMarker) { starts++; start = i; }
            else if (line == EndMarker) { ends++; end = i; }
        }

        if (starts == 0 && ends == 0)
            return true;

        if (starts == 1 && ends == 1 && start < end)
            return true;

        error = $"Hosts file markers are broken ({starts} start, {ends} end). Manual review needed; nothing was changed.";
        return false;
    }

    private IEnumerable<string> BuildBlock()
    {
        yield return StartMarker;
        foreach (string domain in _domains)
            yield return $"0.0.0.0 {domain}";
        yield return EndMarker;
    }

    private void Backup()
    {
        Directory.CreateDirectory(_backupDir);
        string name = $"hosts.{DateTime.Now:yyyyMMdd-HHmmss}.bak";
        File.Copy(_hostsPath, Path.Combine(_backupDir, name), overwrite: true);
    }

    private BlockerResult Fail(string? error) => new(Name, false, error);
}
