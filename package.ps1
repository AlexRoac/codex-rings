param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$version = '1.0.1'
if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'build.ps1') }
& (Join-Path $PSScriptRoot 'test.ps1')
$distDir = Join-Path $PSScriptRoot 'dist'
$bundleDir = Join-Path $distDir "CodexRings-v$version-win-x64"
New-Item -ItemType Directory -Path $bundleDir -Force | Out-Null
$exeSource = Join-Path $PSScriptRoot 'bin\CodexRings.exe'
Copy-Item -LiteralPath $exeSource -Destination (Join-Path $distDir 'CodexRings.exe') -Force
Copy-Item -LiteralPath $exeSource -Destination (Join-Path $bundleDir 'CodexRings.exe') -Force
foreach ($file in @('install.ps1', 'LICENSE', 'README.md', 'CHANGELOG.md', 'SECURITY.md', 'CONTRIBUTING.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $bundleDir $file) -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs') -Destination $bundleDir -Recurse -Force
$zipPath = Join-Path $distDir "CodexRings-v$version-win-x64.zip"
Compress-Archive -Path (Join-Path $bundleDir '*') -DestinationPath $zipPath -Force
$hashLines = foreach ($file in @((Join-Path $distDir 'CodexRings.exe'), $zipPath)) {
    $hash = Get-FileHash -LiteralPath $file -Algorithm SHA256
    '{0}  {1}' -f $hash.Hash.ToLowerInvariant(), [IO.Path]::GetFileName($file)
}
[IO.File]::WriteAllLines((Join-Path $distDir 'SHA256SUMS.txt'), $hashLines, (New-Object Text.UTF8Encoding $false))
Write-Output "Release v$version lista en $distDir"
