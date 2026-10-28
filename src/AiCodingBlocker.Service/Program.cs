using AiCodingBlocker.Core;
using AiCodingBlocker.Core.State;
using AiCodingBlocker.Service;

var builder = Host.CreateApplicationBuilder(args);

// Runs as a Windows service when installed; as a console app when started by hand (for debugging).
builder.Services.AddWindowsService(options => options.ServiceName = "AiCodingBlocker");

builder.Services.AddSingleton(new StateStore(Paths.StatePath));
builder.Services.AddSingleton(sp => new FocusController(
    Paths.ConfigPath, Paths.HostsPath, Paths.BackupDir, sp.GetRequiredService<StateStore>(), DnsCache.Flush));

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<PipeServer>();

builder.Build().Run();
