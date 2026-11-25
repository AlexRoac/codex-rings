param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin'))
$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$outputPath = Join-Path $OutputDirectory 'CodexRings.exe'
$sourceFiles = @(Get-ChildItem -LiteralPath $projectDir -Filter '*.cs' | Sort-Object Name | ForEach-Object FullName)
& $compilerPath /nologo /target:winexe /optimize+ /platform:x64 /out:$outputPath ("/win32manifest:" + (Join-Path $projectDir 'app.manifest')) ("/win32icon:" + (Join-Path $projectDir 'assets\CodexRings.ico')) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.Net.Http.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar Codex Rings.' }
Write-Output $outputPath
