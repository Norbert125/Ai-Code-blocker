using AiCodingBlocker.Core.Blockers;
using AiCodingBlocker.Core.Config;
using AiCodingBlocker.Core.State;

namespace AiCodingBlocker.Core;

/// <summary>
/// The ON/OFF sequence: ConfigLoader → Orchestrator (Online → Local) → StateStore → reply.
/// Keeps the applied blockers between ON and OFF, because LocalBlocker's guard timer lives in them.
/// </summary>
public sealed class FocusController(string configPath, string hostsPath, string backupDir, StateStore state, Action flushDns)
{
    private readonly Lock _lock = new();
    private Orchestrator? _active;
    private DateTime _appliedConfigTime;

    /// <summary>
    /// Turns ON, or, if already ON, reapplies only when config.json changed since it was applied.
    /// The UI sends this at startup so config edits take effect without OFF → ON.
    /// </summary>
    public Reply TurnOn()
    {
        lock (_lock)
        {
            DateTime configTime = File.GetLastWriteTimeUtc(configPath);
            if (_active is not null && configTime == _appliedConfigTime)
                return new Reply(true, []);

            if (!ConfigLoader.TryLoad(configPath, out var config, out string? error))
            {
                // A broken edit while ON keeps the current blocks: a typo never unblocks you.
                return new Reply(_active is not null || state.Load(), [error!]);
            }

            // Config changed while ON: stop the old blockers (incl. the guard timer) before the new ones.
            _active?.TurnOff();

            var orchestrator = Build(config!.Domains, config.ProcessNames, config.GuardIntervalSeconds);
            var result = orchestrator.TurnOn();

            // ON also covers "rollback failed": keep the instance so OFF can retry removing it.
            _active = result.Status == Status.On ? orchestrator : null;
            _appliedConfigTime = configTime;
            return Finish(result);
        }
    }

    public Reply TurnOff()
    {
        lock (_lock)
        {
            // Nothing active (e.g. state was already OFF): Remove() is still safe and cleans leftovers.
            var orchestrator = _active ?? Build([], [], ConfigLoader.MinIntervalSeconds);
            var result = orchestrator.TurnOff();

            if (result.Status == Status.Off)
                _active = null;
            return Finish(result);
        }
    }

    private Orchestrator Build(List<string> domains, List<string> processNames, int intervalSeconds) =>
        new([
            new OnlineBlocker(hostsPath, backupDir, domains),
            new LocalBlocker(processNames, TimeSpan.FromSeconds(intervalSeconds)),
        ]);

    private Reply Finish(OperationResult result)
    {
        bool isOn = result.Status == Status.On;
        state.Save(isOn);
        flushDns();
        return new Reply(isOn, [.. result.Errors]);
    }
}
