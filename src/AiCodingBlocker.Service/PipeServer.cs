using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using AiCodingBlocker.Core;
using AiCodingBlocker.Core.State;

namespace AiCodingBlocker.Service;

/// <summary>
/// Listens on \\.\pipe\AiCodingBlocker. One JSON request line in, one JSON reply line out.
/// Only the signed-in user on this PC can connect; network clients are denied.
/// </summary>
public sealed class PipeServer(FocusController controller, StateStore state, ILogger<PipeServer> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var pipe = CreatePipe();
            await pipe.WaitForConnectionAsync(stoppingToken);

            try
            {
                using var reader = new StreamReader(pipe, leaveOpen: true);
                await using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };

                string? line = await reader.ReadLineAsync(stoppingToken);
                var request = line is null ? null : JsonSerializer.Deserialize<Request>(line, Messages.Json);

                Reply reply = request?.Command switch
                {
                    Command.On => controller.TurnOn(),
                    Command.Off => controller.TurnOff(),
                    _ => new Reply(state.Load(), ["Unknown request."]),
                };

                await writer.WriteLineAsync(JsonSerializer.Serialize(reply, Messages.Json));
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                log.LogWarning(ex, "Pipe request failed.");
            }
        }
    }

    private static NamedPipeServerStream CreatePipe()
    {
        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.InteractiveSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        // Remote connections arrive as NETWORK logons: deny wins over allow.
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));

        return NamedPipeServerStreamAcl.Create(
            Paths.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, security);
    }
}
