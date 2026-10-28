#Requires -RunAsAdministrator
# Installs (or updates) AI Code Blocker:
#   - publishes the service and the UI to Program Files
#   - copies the default config only if none exists (your edits are kept)
#   - registers the Windows service to start with Windows
#   - creates "AI Code Blocker" shortcuts on the Desktop and in the Start menu
# Safe to run again to update.
param(
    [string]$InstallDir = "$env:ProgramFiles\AiCodingBlocker"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dataDir = "$env:ProgramData\AiCodingBlocker"
$serviceName = 'AiCodingBlocker'
$serviceExe = "$InstallDir\AiCodingBlocker.Service.exe"
$uiExe = "$InstallDir\UI\AiCodingBlocker.UI.exe"

# Updating: the running service locks its .exe, so stop it before publishing over it.
$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne 'Stopped') {
    Stop-Service -Name $serviceName
}

dotnet publish "$root\src\AiCodingBlocker.Service" -c Release -o $InstallDir
if ($LASTEXITCODE -ne 0) { throw "Publishing the service failed." }
dotnet publish "$root\src\AiCodingBlocker.UI" -c Release -o "$InstallDir\UI"
if ($LASTEXITCODE -ne 0) { throw "Publishing the UI failed." }

New-Item -ItemType Directory -Force $dataDir | Out-Null
$configPath = "$dataDir\config.json"
if (-not (Test-Path $configPath)) {
    Copy-Item "$root\config\default-config.json" $configPath
}
else {
    # Existing config: add new default domains/processes, keep everything the user added.
    $defaults = Get-Content "$root\config\default-config.json" -Raw | ConvertFrom-Json
    $current = Get-Content $configPath -Raw | ConvertFrom-Json
    $missingDomains = @($defaults.domains | Where-Object { $_ -notin $current.domains })
    $missingProcesses = @($defaults.processNames | Where-Object { $_ -notin $current.processNames })
    if ($missingDomains.Count + $missingProcesses.Count -gt 0) {
        $current.domains = @($current.domains) + $missingDomains
        $current.processNames = @($current.processNames) + $missingProcesses
        $current | ConvertTo-Json -Depth 3 | Set-Content $configPath -Encoding UTF8
        Write-Host "Added to config: $(($missingDomains + $missingProcesses) -join ', ')"
    }
}

if (-not $existing) {
    sc.exe create $serviceName binPath= "`"$serviceExe`"" start= auto DisplayName= "AI Code Blocker" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Registering the service failed (sc.exe exit code $LASTEXITCODE)." }
}
Start-Service -Name $serviceName

# Shortcuts for every user, so nobody needs PowerShell to open the window.
$shell = New-Object -ComObject WScript.Shell
foreach ($folder in @("$env:PUBLIC\Desktop", "$env:ProgramData\Microsoft\Windows\Start Menu\Programs")) {
    $shortcut = $shell.CreateShortcut("$folder\AI Code Blocker.lnk")
    $shortcut.TargetPath = $uiExe
    $shortcut.WorkingDirectory = "$InstallDir\UI"
    $shortcut.Description = 'Block AI assistants while you practise coding'
    $shortcut.Save()
}

Write-Host "AI Code Blocker installed. Open it from the Desktop or Start menu shortcut." -ForegroundColor Green
