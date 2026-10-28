#Requires -RunAsAdministrator
# Uninstalls AI Code Blocker. Turns Focus Mode OFF first so no blocks are left behind.
#   -Force       uninstall even if Focus Mode could not be turned OFF (blocks may remain)
#   -RemoveData  also delete C:\ProgramData\AiCodingBlocker (config, state, hosts backups)
param(
    [string]$InstallDir = "$env:ProgramFiles\AiCodingBlocker",
    [switch]$Force,
    [switch]$RemoveData
)

$ErrorActionPreference = 'Stop'
$serviceName = 'AiCodingBlocker'
$dataDir = "$env:ProgramData\AiCodingBlocker"

# 1. Ask the service to turn OFF (same pipe and message format the UI uses).
function Send-Off {
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream('.', 'AiCodingBlocker', [System.IO.Pipes.PipeDirection]::InOut)
    try {
        $pipe.Connect(3000)
        $writer = New-Object System.IO.StreamWriter($pipe)
        $writer.AutoFlush = $true
        $reader = New-Object System.IO.StreamReader($pipe)
        $writer.WriteLine('{"command":"Off"}')
        return $reader.ReadLine() | ConvertFrom-Json
    }
    finally {
        $pipe.Dispose()
    }
}

$turnedOff = $false
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    try {
        $reply = Send-Off
        if ($reply.isOn) {
            Write-Warning ("Focus Mode is still ON: " + ($reply.errors -join '; '))
        }
        else {
            $turnedOff = $true
        }
    }
    catch {
        Write-Warning "Could not reach the service to turn Focus Mode OFF: $($_.Exception.Message)"
    }
}
else {
    $turnedOff = $true # no service, nothing applied by it
}

if (-not $turnedOff -and -not $Force) {
    Write-Host "Uninstall stopped so the AI blocks aren't left behind." -ForegroundColor Yellow
    Write-Host "Start the service (Start-Service $serviceName) and try again, or rerun with -Force."
    exit 1
}

# 2. Remove the service.
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Stop-Service -Name $serviceName -ErrorAction SilentlyContinue
    sc.exe delete $serviceName | Out-Null
}

# 3. Remove program files and shortcuts.
foreach ($path in @(
        $InstallDir,
        "$env:PUBLIC\Desktop\AI Code Blocker.lnk",
        "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\AI Code Blocker.lnk")) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
}

# 4. Data is kept unless asked for (config edits and hosts backups survive a reinstall).
if ($RemoveData -and (Test-Path $dataDir)) {
    Remove-Item $dataDir -Recurse -Force
}

if ($turnedOff) {
    Write-Host "AI Code Blocker uninstalled. No blocks remain." -ForegroundColor Green
}
else {
    Write-Warning "Uninstalled with -Force. Check the hosts file for a '# >>> AiCodingBlocker' block and remove it if present."
}
