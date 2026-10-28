using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiCodingBlocker.Core;

public enum Command
{
    On,
    Off,
}

/// <summary>UI → service. Sent as one JSON line over the pipe.</summary>
public record Request(Command Command);

/// <summary>Service → UI. IsOn is the real applied state; Errors feed the popup.</summary>
public record Reply(bool IsOn, IReadOnlyList<string> Errors);

public static class Messages
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
