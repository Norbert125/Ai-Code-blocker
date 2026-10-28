using AiCodingBlocker.Core.Blockers;

namespace AiCodingBlocker.Core;

/// <summary>What the orchestrator reports after ON/OFF: the real status plus every blocker result.</summary>
public record OperationResult(Status Status, IReadOnlyList<BlockerResult> Results)
{
    public IEnumerable<string> Errors =>
        Results.Where(r => !r.Ok).Select(r => $"{r.Name}: {r.Error}");
}
