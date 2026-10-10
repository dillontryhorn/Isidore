[CmdletBinding()]
param(
    [string]$AssetsPath = (Join-Path $PSScriptRoot 'obj\project.assets.json'),
    [string]$PackagesPath = (Join-Path $PSScriptRoot '..\packages\.cache'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'bin\Release\net48\THIRD-PARTY-NOTICES.md')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-MetadataValue([System.Xml.XmlNode]$Metadata, [string]$Name) {
    $node = $Metadata.SelectSingleNode("./*[local-name()='$Name']")
    if ($null -eq $node) { return '' }
    return $node.InnerText.Trim()
}

function ConvertTo-MarkdownText([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return 'Not supplied in the package metadata.' }
    return ($Value -replace '\r?\n', ' ').Replace('\', '\\').Replace('[', '\[').Replace(']', '\]').Replace('`', '\`')
}

function ConvertTo-MarkdownPath([string]$Value) {
    $parts = $Value.Replace('\', '/').Split('/')
    return (($parts | ForEach-Object { [Uri]::EscapeDataString($_) }) -join '/')
}

function Get-ContainedPath([string]$Root, [string]$RelativePath) {
    $rootFullPath = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $fullPath = [IO.Path]::GetFullPath((Join-Path $rootFullPath $RelativePath))
    $prefix = $rootFullPath + [IO.Path]::DirectorySeparatorChar
    if (!$fullPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Package path is outside its expected directory: $RelativePath"
    }
    return $fullPath
}

$resolvedAssetsPath = (Resolve-Path -LiteralPath $AssetsPath).ProviderPath
$resolvedPackagesPath = (Resolve-Path -LiteralPath $PackagesPath).ProviderPath
$resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [IO.Path]::GetDirectoryName($resolvedOutputPath)
$noticesDirectory = Join-Path $outputDirectory 'notices'
New-Item -ItemType Directory -Path $noticesDirectory -Force | Out-Null
$assets = Get-Content -LiteralPath $resolvedAssetsPath -Raw | ConvertFrom-Json

$mitPermission = @'
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
'@

$utf8 = New-Object System.Text.UTF8Encoding($false)
$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# Benchmark tooling third-party notices')
$lines.Add('')
$lines.Add('Generated from the restored `obj/project.assets.json`. Only the resolved package IDs and versions are listed; other versions in the package cache are excluded.')
$lines.Add('')
$lines.Add('This manifest covers development and benchmark tooling. Production performance dependencies are System.Buffers 4.6.1 and System.Numerics.Vectors 4.6.1; their runtime notice is maintained separately in the repository root.')
$lines.Add('')
$lines.Add('Original package license and notice files are copied without conversion, including files containing RTF. Package nuspec files preserve the supplied metadata. When MIT is declared without an embedded license, a generated MIT permission file includes the supplied copyright and author metadata.')
$lines.Add('')
$lines.Add('Redistribute this manifest together with the linked notice files when redistributing this tooling, including the preserved upstream native Capstone license texts. This script does not download external files or delete existing output files.')
$lines.Add('')

$packageCount = 0
$unknownLicenses = New-Object 'System.Collections.Generic.List[string]'
$resolvedLibraries = @($assets.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' } | Sort-Object Name)
foreach ($library in $resolvedLibraries) {
    $packageRoot = Get-ContainedPath $resolvedPackagesPath ([string]$library.Value.path)
    $nuspecFile = Get-ChildItem -LiteralPath $packageRoot -Filter '*.nuspec' -File | Sort-Object Name | Select-Object -First 1
    if ($null -eq $nuspecFile) { throw "Missing restored nuspec for $($library.Name). Restore the benchmark project first." }
    [xml]$nuspec = Get-Content -LiteralPath $nuspecFile.FullName -Raw
    $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    if ($null -eq $metadata) { throw "Missing nuspec metadata for $($library.Name)." }

    $packageId = Get-MetadataValue $metadata 'id'
    $packageVersion = Get-MetadataValue $metadata 'version'
    if (($packageId + '/' + $packageVersion) -ine $library.Name) {
        throw "Restored package metadata does not match the resolved asset: $($library.Name)."
    }
    $authors = Get-MetadataValue $metadata 'authors'
    $copyright = Get-MetadataValue $metadata 'copyright'
    $projectUrl = Get-MetadataValue $metadata 'projectUrl'
    $licenseUrl = Get-MetadataValue $metadata 'licenseUrl'
    $licenseNode = $metadata.SelectSingleNode("./*[local-name()='license']")
    $licenseType = ''
    $licenseValue = ''
    if ($null -ne $licenseNode) {
        $licenseType = $licenseNode.GetAttribute('type')
        $licenseValue = $licenseNode.InnerText.Trim()
    }

    $packageNoticeRelative = 'notices/' + $packageId + '/' + $packageVersion
    $packageNoticeDirectory = Get-ContainedPath $outputDirectory $packageNoticeRelative
    New-Item -ItemType Directory -Path $packageNoticeDirectory -Force | Out-Null
    $metadataDestination = Get-ContainedPath $packageNoticeDirectory $nuspecFile.Name
    Copy-Item -LiteralPath $nuspecFile.FullName -Destination $metadataDestination -Force

    $originalFiles = @(Get-ChildItem -LiteralPath $packageRoot -Recurse -File | Where-Object { $_.Name -match '(?i)(licen[cs]e|notice|copying|copyright)' })
    if ($licenseType -eq 'file') {
        $declaredLicense = Get-ContainedPath $packageRoot $licenseValue
        if (!(Test-Path -LiteralPath $declaredLicense -PathType Leaf)) {
            throw "The embedded license declared by $($library.Name) is missing: $licenseValue"
        }
        $discoveredPaths = @($originalFiles | ForEach-Object { $_.FullName })
        if ($discoveredPaths -notcontains $declaredLicense) {
            $originalFiles += Get-Item -LiteralPath $declaredLicense
        }
    }

    $lines.Add('## ' + $packageId + ' ' + $packageVersion)
    $lines.Add('')
    $lines.Add('- Authors: ' + (ConvertTo-MarkdownText $authors))
    $lines.Add('- Copyright: ' + (ConvertTo-MarkdownText $copyright))
    if ($licenseValue) {
        $lines.Add('- License metadata: ' + (ConvertTo-MarkdownText ($licenseType + ': ' + $licenseValue)))
    } else {
        $lines.Add('- License metadata: No expression or embedded-file declaration supplied.')
    }
    if ($licenseUrl) {
        $lines.Add('- Package license URL: [supplied license URL](<' + $licenseUrl + '>)')
    } else {
        $lines.Add('- Package license URL: Not supplied.')
    }
    if ($projectUrl) {
        $lines.Add('- Project URL: [supplied project URL](<' + $projectUrl + '>)')
    } else {
        $lines.Add('- Project URL: Not supplied.')
    }
    $repository = $metadata.SelectSingleNode("./*[local-name()='repository']")
    if ($null -ne $repository -and $repository.GetAttribute('url')) {
        $repositoryUrl = $repository.GetAttribute('url')
        $lines.Add('- Source repository: [supplied repository URL](<' + $repositoryUrl + '>)')
        if ($repository.GetAttribute('commit')) {
            $lines.Add('- Source commit: ' + (ConvertTo-MarkdownText $repository.GetAttribute('commit')))
        }
    }
    $metadataLink = ConvertTo-MarkdownPath ($packageNoticeRelative + '/' + $nuspecFile.Name)
    $lines.Add('- Original package metadata: [nuspec](' + $metadataLink + ')')

    $hasEmbeddedLicense = $licenseType -eq 'file'
    foreach ($file in ($originalFiles | Sort-Object FullName)) {
        $relativeFile = $file.FullName.Substring($packageRoot.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $destination = Get-ContainedPath $packageNoticeDirectory $relativeFile
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
        if ($file.Name -match '(?i)(licen[cs]e|copying)') { $hasEmbeddedLicense = $true }
        $noticeLink = ConvertTo-MarkdownPath ($packageNoticeRelative + '/' + $relativeFile)
        $lines.Add('- Original license/notice: [' + (ConvertTo-MarkdownText $relativeFile) + '](' + $noticeLink + ')')
    }

    if ($licenseType -eq 'expression' -and $licenseValue -eq 'MIT' -and !$hasEmbeddedLicense) {
        $mitText = 'MIT License' + [Environment]::NewLine + [Environment]::NewLine
        if ($copyright) { $mitText += $copyright + [Environment]::NewLine }
        if ($authors) { $mitText += 'Authors (package metadata): ' + $authors + [Environment]::NewLine }
        if (@('System.Buffers', 'System.Numerics.Vectors') -contains $packageId -and $packageVersion -eq '4.6.1') {
            $runtimeNoticeSource = Join-Path $PSScriptRoot '..\THIRD-PARTY-NOTICES.md'
            if (!(Test-Path -LiteralPath $runtimeNoticeSource -PathType Leaf)) {
                throw 'The source runtime notice is missing; cannot preserve its additional attribution.'
            }
            $foundationCopyright = @(Get-Content -LiteralPath $runtimeNoticeSource | Where-Object { $_ -eq 'Copyright (c) .NET Foundation and Contributors' })
            if ($foundationCopyright.Count -ne 1) {
                throw 'The .NET Foundation source copyright is missing or ambiguous in the runtime notice.'
            }
            $mitText += $foundationCopyright[0] + [Environment]::NewLine
            $sourceNoticeFileName = 'SOURCE-RUNTIME-NOTICE.md'
            Copy-Item -LiteralPath $runtimeNoticeSource -Destination (Join-Path $packageNoticeDirectory $sourceNoticeFileName) -Force
            $sourceNoticeLink = ConvertTo-MarkdownPath ($packageNoticeRelative + '/' + $sourceNoticeFileName)
            $lines.Add('- Additional source attribution: [runtime source notice](' + $sourceNoticeLink + ') (.NET Foundation and Contributors).')
        }
        $mitText += [Environment]::NewLine + $mitPermission + [Environment]::NewLine
        $mitFileName = 'GENERATED-MIT-LICENSE.txt'
        [IO.File]::WriteAllText((Join-Path $packageNoticeDirectory $mitFileName), $mitText, $utf8)
        $mitLink = ConvertTo-MarkdownPath ($packageNoticeRelative + '/' + $mitFileName)
        $lines.Add('- MIT permission text with supplied attribution: [generated license](' + $mitLink + ')')
    }

    if (!$licenseValue -and !$hasEmbeddedLicense -and !$licenseUrl) {
        $unknownLicenses.Add($library.Name)
        $lines.Add('- **UNKNOWN LICENSE: no license expression, embedded license, or license URL was supplied. Resolve this before redistributing the package.**')
    }
    if ($packageId -eq 'System.Runtime.InteropServices.RuntimeInformation' -and $packageVersion -eq '4.0.0') {
        $lines.Add('- License note: The preserved `dotnet_library_license.txt` contains the Microsoft .NET Library License for this binary package, in RTF format. These terms are not labeled MIT.')
    }
    if ($packageId -eq 'Gee.External.Capstone') {
        if ($packageVersion -ne '2.3.0') {
            throw 'Native Capstone license provenance was verified for Gee.External.Capstone 2.3.0. Review a changed version before exporting its native notices.'
        }
        $nativeLicenseSource = Join-Path $PSScriptRoot 'licenses\Capstone-4.0.2'
        $nativeNoticeDirectory = Join-Path $packageNoticeDirectory 'native-capstone'
        New-Item -ItemType Directory -Path $nativeNoticeDirectory -Force | Out-Null
        foreach ($nativeFileName in @('LICENSE.TXT', 'LICENSE_LLVM.TXT', 'README.md')) {
            $nativeFileSource = Join-Path $nativeLicenseSource $nativeFileName
            if (!(Test-Path -LiteralPath $nativeFileSource -PathType Leaf)) {
                throw "The preserved upstream Capstone notice is missing: $nativeFileName"
            }
            Copy-Item -LiteralPath $nativeFileSource -Destination (Join-Path $nativeNoticeDirectory $nativeFileName) -Force
        }
        $lines.Add('')
        $lines.Add('### Bundled native component: Capstone engine 4')
        $lines.Add('')
        $lines.Add('Attribution: Copyright (c) 2013, COSEINC; designed and implemented by Nguyen Anh Quynh. LLVM-derived code: Copyright (c) 2003-2013 University of Illinois at Urbana-Champaign, developed by the LLVM Team. The wrapper declares MIT; the native engine has separate BSD-3-Clause and University of Illinois/NCSA terms, preserved below.')
        $lines.Add('')
        $nativeLicenseLink = ConvertTo-MarkdownPath ($packageNoticeRelative + '/native-capstone/LICENSE.TXT')
        $nativeLlvmLicenseLink = ConvertTo-MarkdownPath ($packageNoticeRelative + '/native-capstone/LICENSE_LLVM.TXT')
        $nativeProvenanceLink = ConvertTo-MarkdownPath ($packageNoticeRelative + '/native-capstone/README.md')
        $lines.Add('- Preserved native Capstone license: [LICENSE.TXT](' + $nativeLicenseLink + ').')
        $lines.Add('- Preserved LLVM-derived component license: [LICENSE_LLVM.TXT](' + $nativeLlvmLicenseLink + ').')
        $lines.Add('- Native version verification and license provenance: [record](' + $nativeProvenanceLink + ').')
        $lines.Add('- Upstream Capstone 4.0.2 license reference: [LICENSE.TXT](https://raw.githubusercontent.com/capstone-engine/capstone/4.0.2/LICENSE.TXT).')
        $lines.Add('- Upstream LLVM-derived component license reference: [LICENSE_LLVM.TXT](https://raw.githubusercontent.com/capstone-engine/capstone/4.0.2/LICENSE_LLVM.TXT).')
        $lines.Add('')
        $lines.Add('The restored win-x64 native binary reports API version 4.0 through `cs_version`; PE and NuGet metadata do not identify its patch version. The 4.0.2 tag identifies the preserved upstream license snapshot, not an asserted binary patch version. Include these local native copyright notices, license conditions and disclaimers when redistributing the native binaries. The exporter copies the tracked upstream texts without downloading them.')
    }
    $lines.Add('')
    $packageCount++
}

$lines.Add('## Generation summary')
$lines.Add('')
$lines.Add('Resolved NuGet packages: ' + $packageCount + '.')
$lines.Add('Packages without supplied license information: ' + $unknownLicenses.Count + '.')
[IO.File]::WriteAllLines($resolvedOutputPath, $lines, $utf8)
foreach ($unknown in $unknownLicenses) { Write-Warning "Unknown package license: $unknown" }
Write-Output "Exported notices for $packageCount resolved packages to $resolvedOutputPath"
