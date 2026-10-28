using System.Diagnostics;

namespace AiCodingBlocker.Service;

public static class DnsCache
{
    /// <summary>Makes Windows forget cached lookups so hosts-file changes apply immediately.</summary>
    public static void Flush()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("ipconfig", "/flushdns")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            });
            process?.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // Not fatal: the hosts change still applies once cached entries expire.
        }
    }
}
