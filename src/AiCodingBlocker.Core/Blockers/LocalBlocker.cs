using System.ComponentModel;
using System.Diagnostics;

namespace AiCodingBlocker.Core.Blockers;

/// <summary>
/// Stops configured local AI programs (Ollama) and keeps stopping them while ON.
/// OFF only stops the guard; it never restarts anything.
/// </summary>
public sealed class LocalBlocker : IBlocker
{
    private readonly IReadOnlyList<string> _processNames;
    private readonly TimeSpan _interval;
    private Timer? _guard;

    /// <param name="processNames">Process names without ".exe", e.g. "ollama app", "ollama".</param>
    /// <param name="interval">How often the guard re-checks while ON.</param>
    public LocalBlocker(IEnumerable<string> processNames, TimeSpan interval)
    {
        _processNames = processNames.ToList();
        _interval = interval;
    }

    public string Name => "Local";

    public BlockerResult Apply()
    {
        string? error = KillAll();
        if (error is not null)
            return new BlockerResult(Name, false, error);

        // ??= keeps a second Apply from starting a second guard.
        _guard ??= new Timer(_ => KillAll(), null, _interval, _interval);
        return new BlockerResult(Name, true);
    }

    public BlockerResult Remove()
    {
        _guard?.Dispose();
        _guard = null;
        return new BlockerResult(Name, true);
    }

    /// <summary>Kills every running match. Returns an error message, or null if all went fine.</summary>
    // ponytail: guard-tick errors are dropped for now; log them once the service has logging.
    private string? KillAll()
    {
        string? error = null;
        foreach (string name in _processNames)
        {
            foreach (Process process in Process.GetProcessesByName(name))
            {
                using (process)
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch (InvalidOperationException) { } // exited on its own meanwhile
                    catch (Win32Exception ex) { error = $"Could not stop {name}: {ex.Message}"; }
                }
            }
        }
        return error;
    }
}
