param(
    [string]$MSBuildPath,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$RequireGpu,
    [switch]$CpuOnly,
    [switch]$Benchmark
)
$ErrorActionPreference = 'Stop'
if ($CpuOnly -and ($RequireGpu -or $Benchmark)) {
    throw '-CpuOnly cannot be combined with -RequireGpu or -Benchmark.'
}
$repositoryPath = Split-Path -Parent $PSScriptRoot

if (-not $MSBuildPath) {
    $command = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($command) { $MSBuildPath = $command.Source }
    else {
        $vswherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path -LiteralPath $vswherePath) {
            $MSBuildPath = & $vswherePath -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        }
    }
}
if (-not $MSBuildPath) { throw 'Windows MSBuild was not found. Run from a Visual Studio Developer PowerShell or pass -MSBuildPath.' }

# Explicit SDK properties avoid an unnecessary SDK inventory lookup in restricted sessions.
& $MSBuildPath (Join-Path $PSScriptRoot 'RegressionTests.csproj') /t:Build "/p:Configuration=$Configuration" /p:Platform=AnyCPU "/p:TargetPlatformSdkPath=$repositoryPath" /p:TargetPlatformDisplayName=Windows /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "Regression build failed with exit code $LASTEXITCODE." }
$runnerArguments = @()
if ($RequireGpu) { $runnerArguments += '--require-gpu' }
if ($CpuOnly) { $runnerArguments += '--cpu' }
if ($Benchmark) { $runnerArguments += '--benchmark' }
& (Join-Path $PSScriptRoot "bin\$Configuration\Isidore.RegressionTests.exe") @runnerArguments
if ($LASTEXITCODE -ne 0) { throw "Regression checks failed with exit code $LASTEXITCODE." }
