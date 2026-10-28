using AiCodingBlocker.Core.Blockers;

namespace AiCodingBlocker.Core;

/// <summary>
/// Runs the blockers in order. Knows the ON/OFF rules, nothing about pipes, files or processes.
/// </summary>
public sealed class Orchestrator(IEnumerable<IBlocker> blockers)
{
    private readonly IReadOnlyList<IBlocker> _blockers = [.. blockers];

    /// <summary>
    /// Applies every blocker. If one fails, removes the ones that succeeded (reverse order).
    /// OFF if the rollback is clean, ON if something could not be removed.
    /// </summary>
    public OperationResult TurnOn()
    {
        var results = new List<BlockerResult>();
        var applied = new Stack<IBlocker>();

        foreach (var blocker in _blockers)
        {
            var result = blocker.Apply();
            results.Add(result);

            if (!result.Ok)
                return Rollback(applied, results);

            applied.Push(blocker);
        }

        return new OperationResult(Status.On, results);
    }

    /// <summary>Removes every blocker. Anything that fails to come off keeps the status ON.</summary>
    public OperationResult TurnOff()
    {
        var results = _blockers.Select(b => b.Remove()).ToList();
        var status = results.All(r => r.Ok) ? Status.Off : Status.On;
        return new OperationResult(status, results);
    }

    private static OperationResult Rollback(Stack<IBlocker> applied, List<BlockerResult> results)
    {
        bool clean = true;
        while (applied.TryPop(out var blocker)) // Stack pops last-applied first
        {
            var removed = blocker.Remove();
            if (!removed.Ok)
            {
                clean = false;
                results.Add(removed);
            }
        }

        return new OperationResult(clean ? Status.Off : Status.On, results);
    }
}
