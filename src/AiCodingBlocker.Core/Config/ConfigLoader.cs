using System.Text.Json;

namespace AiCodingBlocker.Core.Config;

/// <summary>
/// Loads config.json and rejects anything that would silently not block (fail fast).
/// Runs before any blocker, so a typo never half-applies.
/// </summary>
public static class ConfigLoader
{
    public const int MinIntervalSeconds = 5;
    public const int MaxIntervalSeconds = 3600;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static bool TryLoad(string path, out AppConfig? config, out string? error)
    {
        config = null;
        error = null;

        AppConfig? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), Json);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            error = $"Could not read {path}: {ex.Message}";
            return false;
        }

        var problems = Validate(parsed);
        if (problems.Count > 0)
        {
            error = string.Join(Environment.NewLine, problems) + Environment.NewLine + "Check config.json for typos.";
            return false;
        }

        config = parsed;
        return true;
    }

    private static List<string> Validate(AppConfig? config)
    {
        if (config?.Domains is null || config.ProcessNames is null)
            return ["config.json must contain \"domains\" and \"processNames\"."];

        var problems = new List<string>();

        foreach (string domain in config.Domains)
        {
            // Uri.CheckHostName rejects "https://...", paths and spaces; the dot rejects bare words.
            if (Uri.CheckHostName(domain) != UriHostNameType.Dns || !domain.Contains('.'))
                problems.Add($"Invalid domain \"{domain}\": use a bare hostname like \"chatgpt.com\".");
        }

        foreach (string name in config.ProcessNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                problems.Add("Empty process name.");
            else if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                problems.Add($"Invalid process name \"{name}\": drop the \".exe\".");
        }

        if (config.GuardIntervalSeconds is < MinIntervalSeconds or > MaxIntervalSeconds)
            problems.Add($"guardIntervalSeconds must be {MinIntervalSeconds}-{MaxIntervalSeconds}, got {config.GuardIntervalSeconds}.");

        return problems;
    }
}
