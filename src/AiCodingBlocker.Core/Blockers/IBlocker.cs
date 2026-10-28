namespace AiCodingBlocker.Core.Blockers;

/// <summary>One kind of restriction the orchestrator can turn on and off.</summary>
public interface IBlocker
{
    string Name { get; }

    /// <summary>Turns the restriction on. Must be safe to call repeatedly.</summary>
    BlockerResult Apply();

    /// <summary>Removes only what this blocker added. Must be safe to call repeatedly.</summary>
    BlockerResult Remove();
}
