namespace AiCodingBlocker.Core.Blockers;

public record BlockerResult(string Name, bool Ok, string? Error = null);
