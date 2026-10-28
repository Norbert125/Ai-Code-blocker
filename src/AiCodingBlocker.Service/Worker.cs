using AiCodingBlocker.Core;
using AiCodingBlocker.Core.State;

namespace AiCodingBlocker.Service;

/// <summary>At boot: if Focus Mode was ON before the restart, apply it again.</summary>
public sealed class Worker(FocusController controller, StateStore state, ILogger<Worker> log) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!state.Load())
        {
            log.LogInformation("Saved state is OFF; nothing to reapply.");
            return Task.CompletedTask;
        }

        var reply = controller.TurnOn();
        if (reply.Errors.Count > 0)
            log.LogWarning("Reapply at boot: IsOn={IsOn}, errors: {Errors}", reply.IsOn, string.Join("; ", reply.Errors));
        else
            log.LogInformation("Reapplied Focus Mode at boot.");

        return Task.CompletedTask;
    }
}
