using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using AiCodingBlocker.Core;

namespace AiCodingBlocker.UI;

/// <summary>Mirror of PipeServer: one request line out, one reply line in.</summary>
public static class PipeClient
{
    private const int ConnectTimeoutMs = 2000;

    /// <exception cref="TimeoutException">The service isn't running.</exception>
    public static async Task<Reply> SendAsync(Command command)
    {
        await using var pipe = new NamedPipeClientStream(".", Paths.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(ConnectTimeoutMs);

        using var reader = new StreamReader(pipe, leaveOpen: true);
        await using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };

        await writer.WriteLineAsync(JsonSerializer.Serialize(new Request(command), Messages.Json));
        string? line = await reader.ReadLineAsync();

        return (line is null ? null : JsonSerializer.Deserialize<Reply>(line, Messages.Json))
            ?? throw new IOException("The service closed the connection without replying.");
    }
}
