param([switch]$Diagnostics)
$ErrorActionPreference = 'Stop'
$binaryPath = Join-Path $PSScriptRoot 'CodexRings.exe'
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'build.ps1')) {
    & (Join-Path $PSScriptRoot 'build.ps1')
    $binaryPath = Join-Path $PSScriptRoot 'bin\CodexRings.exe'
}
if (-not (Test-Path -LiteralPath $binaryPath)) { throw 'No se encontró CodexRings.exe junto al instalador.' }
$installDir = Join-Path $env:LOCALAPPDATA 'CodexRings'
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
$runningApps = Get-Process -Name CodexRings -ErrorAction SilentlyContinue
foreach ($runningApp in $runningApps) {
    if ($runningApp.Path -eq (Join-Path $installDir 'CodexRings.exe')) { Stop-Process -Id $runningApp.Id; $runningApp.WaitForExit(5000) | Out-Null }
}
Copy-Item -LiteralPath $binaryPath -Destination (Join-Path $installDir 'CodexRings.exe') -Force
$codexCommand = Get-Command codex -ErrorAction SilentlyContinue
if ($codexCommand) { Set-Content -LiteralPath (Join-Path $installDir 'codex-path.txt') -Value $codexCommand.Source }
$shellObject = New-Object -ComObject WScript.Shell
$startupDir = [Environment]::GetFolderPath('Startup')
$desktopDir = [Environment]::GetFolderPath('Desktop')
foreach ($shortcutDir in @($startupDir, $desktopDir)) {
    $shortcut = $shellObject.CreateShortcut((Join-Path $shortcutDir 'Codex Rings.lnk'))
    $shortcut.TargetPath = Join-Path $installDir 'CodexRings.exe'
    if ($shortcutDir -eq $startupDir) { $shortcut.Arguments = '--background' }
    $shortcut.WorkingDirectory = $installDir
    $shortcut.Description = 'Límites reales de Codex: cinco horas y semanal'
    $shortcut.Save()
}
$launchArguments = @('--background')
if ($Diagnostics) { $launchArguments += '--diagnostics' }
Start-Process -FilePath (Join-Path $installDir 'CodexRings.exe') -ArgumentList $launchArguments -WindowStyle Hidden
Write-Output "Instalada e iniciada: $installDir"
