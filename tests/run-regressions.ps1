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

# Restore the pinned runtime packages when running from a fresh checkout.
$buffersDll = Join-Path $repositoryPath 'packages\System.Buffers.4.6.1\lib\net462\System.Buffers.dll'
$vectorsDll = Join-Path $repositoryPath 'packages\System.Numerics.Vectors.4.6.1\lib\net462\System.Numerics.Vectors.dll'
if (-not (Test-Path -LiteralPath $buffersDll) -or -not (Test-Path -LiteralPath $vectorsDll)) {
    & $MSBuildPath (Join-Path $repositoryPath 'Isidore.Maths\Isidore.Maths.csproj') /t:Restore /p:RestorePackagesConfig=true "/p:RestoreRepositoryPath=$repositoryPath\packages" "/p:RestoreConfigFile=$repositoryPath\NuGet.Config" "/p:TargetPlatformSdkPath=$repositoryPath" /p:TargetPlatformDisplayName=Windows /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw "Runtime dependency restore failed with exit code $LASTEXITCODE." }
}

# Explicit SDK properties avoid an unnecessary SDK inventory lookup in restricted sessions.
& $MSBuildPath (Join-Path $PSScriptRoot 'RegressionTests.csproj') /t:Build "/p:Configuration=$Configuration" /p:Platform=AnyCPU "/p:TargetPlatformSdkPath=$repositoryPath" /p:TargetPlatformDisplayName=Windows /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "Regression build failed with exit code $LASTEXITCODE." }
$runnerArguments = @()
if ($RequireGpu) { $runnerArguments += '--require-gpu' }
if ($CpuOnly) { $runnerArguments += '--cpu' }
if ($Benchmark) { $runnerArguments += '--benchmark' }
& (Join-Path $PSScriptRoot "bin\$Configuration\Isidore.RegressionTests.exe") @runnerArguments
if ($LASTEXITCODE -ne 0) { throw "Regression checks failed with exit code $LASTEXITCODE." }
