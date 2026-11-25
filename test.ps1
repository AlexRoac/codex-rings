param([string]$Executable = (Join-Path $PSScriptRoot 'bin\CodexRings.exe'))
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Executable)) { & (Join-Path $PSScriptRoot 'build.ps1') }
$testRun = Start-Process -FilePath $Executable -ArgumentList '--self-test' -Wait -PassThru
if ($testRun.ExitCode -ne 0) { throw "Las comprobaciones fallaron con código $($testRun.ExitCode)." }
Get-Content -LiteralPath (Join-Path $env:LOCALAPPDATA 'CodexRings\test-results.txt')
