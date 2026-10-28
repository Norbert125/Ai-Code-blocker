namespace AiCodingBlocker.Core.Config;

/// <param name="Domains">Hostnames for the hosts file, e.g. "chatgpt.com". No scheme, no path, no wildcards.</param>
/// <param name="ProcessNames">Process names without ".exe", e.g. "ollama".</param>
/// <param name="GuardIntervalSeconds">How often the local guard re-checks while ON.</param>
public record AppConfig(List<string> Domains, List<string> ProcessNames, int GuardIntervalSeconds);
