param(
    [string]$MSBuildPath,
    [string[]]$Filter = @('*')
)
$ErrorActionPreference = 'Stop'
$repositoryPath = Split-Path -Parent $PSScriptRoot

# Build and verify the Release DLLs that this isolated project measures.
& (Join-Path $repositoryPath 'tests\run-regressions.ps1') -Configuration Release -CpuOnly -MSBuildPath $MSBuildPath
if ($LASTEXITCODE -ne 0) { throw "Regression checks failed with exit code $LASTEXITCODE." }

$benchmarkProject = Join-Path $PSScriptRoot 'Isidore.Benchmarks.csproj'
& dotnet restore $benchmarkProject --configfile (Join-Path $repositoryPath 'NuGet.Config') --locked-mode --verbosity minimal "/p:TargetPlatformSdkPath=$repositoryPath" /p:TargetPlatformDisplayName=Windows
if ($LASTEXITCODE -ne 0) { throw "Benchmark restore failed with exit code $LASTEXITCODE." }
& dotnet build $benchmarkProject --configuration Release --no-restore --verbosity minimal "/p:TargetPlatformSdkPath=$repositoryPath" /p:TargetPlatformDisplayName=Windows
if ($LASTEXITCODE -ne 0) { throw "Benchmark build failed with exit code $LASTEXITCODE." }

& (Join-Path $PSScriptRoot 'export-notices.ps1')
Push-Location $repositoryPath
try {
    & (Join-Path $PSScriptRoot 'bin\Release\net48\Isidore.Benchmarks.exe') --filter @Filter
    if ($LASTEXITCODE -ne 0) { throw "Benchmarks failed with exit code $LASTEXITCODE." }
}
finally { Pop-Location }
