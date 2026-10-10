param([string]$MSBuildPath)
$ErrorActionPreference = 'Stop'
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
& $MSBuildPath (Join-Path $PSScriptRoot 'RegressionTests.csproj') /t:Build /p:Configuration=Debug /p:Platform=AnyCPU "/p:TargetPlatformSdkPath=$repositoryPath" /p:TargetPlatformDisplayName=Windows /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "Regression build failed with exit code $LASTEXITCODE." }
& (Join-Path $PSScriptRoot 'bin\Debug\Isidore.RegressionTests.exe')
if ($LASTEXITCODE -ne 0) { throw "Regression checks failed with exit code $LASTEXITCODE." }
