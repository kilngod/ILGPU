<#
.SYNOPSIS
    End-to-end consumer smoke check for the ILGPUC NuGet package on Windows.

.DESCRIPTION
    Packs ILGPU and ILGPUC into a local feed, restores and builds HelloKernel
    against that feed, runs it, and checks its output and code-generation files.

.PARAMETER Configuration
    Build configuration. Defaults to the CONFIG environment variable or Release.

.PARAMETER Version
    Package version. Defaults to VERSION or a timestamped E2E version.
#>
[CmdletBinding()]
param(
    [string] $Configuration = $(if ($env:CONFIG) { $env:CONFIG } else { 'Release' }),
    [string] $Version = $(if ($env:VERSION) {
        $env:VERSION
    } else {
        '0.0.0-e2e-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
    })
)

$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testDir = $PSScriptRoot
$projectDir = Join-Path $testDir 'HelloKernel'
$feed = Join-Path $testDir 'feed'
$rid = 'win-x64'
$staging = Join-Path $repoRoot 'Bin\Packaging\ILGPUC-staging'
$ridOutput = Join-Path $staging "tools\net10.0\$rid"
$jitOutput = Join-Path $staging 'tools\net10.0'
$expectedOutput = @(
    '0 = 50'
    '1 = 27'
    '2 = -4'
    '3 = -15'
    '4 = 48'
    '5 = -8'
    '6 = 0'
    '7 = 0'
) -join "`n"

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]] $Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Remove-DirectoryWithinRoot {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Root
    )

    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith(
        $resolvedRoot,
        [StringComparison]::OrdinalIgnoreCase
    )) {
        throw "Refusing to remove path outside '$resolvedRoot': '$resolvedPath'."
    }
    if (Test-Path -LiteralPath $resolvedPath) {
        Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    }
}

Write-Host '==> EndToEndTest (Windows)'
Write-Host "    repo:    $repoRoot"
Write-Host "    config:  $Configuration"
Write-Host "    version: $Version"
Write-Host "    rid:     $rid"
Write-Host "    feed:    $feed"
Write-Host

# Start clean, matching run.sh. These paths are confined to EndToEndTest.
foreach ($path in @(
    $feed
    (Join-Path $projectDir 'packages')
    (Join-Path $projectDir 'bin')
    (Join-Path $projectDir 'obj')
    (Join-Path $projectDir 'NuGet.config')
)) {
    Remove-DirectoryWithinRoot -Path $path -Root $testDir
}
New-Item -ItemType Directory -Path $feed -Force | Out-Null

Write-Host '==> pack ILGPU'
Invoke-DotNet @(
    'pack', (Join-Path $repoRoot 'Src\ILGPU\ILGPU.csproj'),
    '--configuration', $Configuration,
    '--output', $feed,
    "-p:Version=$Version",
    "-p:PackageVersion=$Version",
    '-p:IncludeSymbols=false',
    '--nologo', '--verbosity', 'minimal'
)

# Build the same ILGPUC package layout as pack-ilgpuc.sh, for win-x64 only:
# a ReadyToRun host binary plus the RID-agnostic JIT fallback.
Write-Host '==> publish ILGPUC (win-x64, ReadyToRun + JIT fallback)'
Remove-DirectoryWithinRoot -Path $staging -Root (Join-Path $repoRoot 'Bin')
New-Item -ItemType Directory -Path $ridOutput -Force | Out-Null
$ilgpucProject = Join-Path $repoRoot 'Src\ILGPUC\ILGPUC.csproj'

Invoke-DotNet @(
    'publish', $ilgpucProject,
    '--configuration', $Configuration,
    '--framework', 'net10.0',
    '--runtime', $rid,
    '--self-contained', 'false',
    '-p:PublishReadyToRun=true',
    '-p:PublishReadyToRunComposite=false',
    '-p:DebugType=none',
    '-p:DebugSymbols=false',
    '-p:_ILGPUCToolDepPrivateAssets=',
    '--output', $ridOutput,
    '--nologo', '--verbosity', 'minimal'
)
Get-ChildItem -LiteralPath $ridOutput -File |
    Where-Object { $_.Extension -in @('.xml', '.pdb') } |
    Remove-Item -Force

Write-Host '==> publish JIT fallback (RID-agnostic)'
Invoke-DotNet @(
    'publish', $ilgpucProject,
    '--configuration', $Configuration,
    '--framework', 'net10.0',
    '--self-contained', 'false',
    '-p:DebugType=none',
    '-p:DebugSymbols=false',
    '-p:_ILGPUCToolDepPrivateAssets=',
    '--output', $jitOutput,
    '--nologo', '--verbosity', 'minimal'
)
Get-ChildItem -LiteralPath $jitOutput -File -Filter '*.xml' |
    Remove-Item -Force

Write-Host '==> dedupe ReadyToRun files against JIT fallback'
Get-ChildItem -LiteralPath $ridOutput -File -Filter '*.dll' | ForEach-Object {
    $fallbackFile = Join-Path $jitOutput $_.Name
    if ((Test-Path -LiteralPath $fallbackFile) -and
        ( (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash -eq
          (Get-FileHash -LiteralPath $fallbackFile -Algorithm SHA256).Hash )) {
        Remove-Item -LiteralPath $_.FullName -Force
    }
}

Write-Host '==> pack ILGPUC'
Invoke-DotNet @(
    'pack', $ilgpucProject,
    '--configuration', $Configuration,
    '--output', $feed,
    "-p:PackageVersion=$Version",
    "-p:Version=$Version",
    "-p:ILGPUCPackStagingDir=$staging",
    '-p:IncludeSymbols=false',
    '--nologo', '--verbosity', 'minimal'
)

Write-Host '==> render NuGet.config'
$templatePath = Join-Path $testDir 'NuGet.config.template'
$nugetConfigPath = Join-Path $projectDir 'NuGet.config'
$escapedFeed = [Security.SecurityElement]::Escape($feed)
$template = Get-Content -LiteralPath $templatePath -Raw
$renderedConfig = $template.Replace('@@FEED@@', $escapedFeed)
Set-Content -LiteralPath $nugetConfigPath -Value $renderedConfig -Encoding utf8

Write-Host '==> restore + build HelloKernel'
Push-Location $projectDir
try {
    Invoke-DotNet @(
        'restore',
        "-p:ILGPUC_PACKAGE_VERSION=$Version",
        '--nologo', '--verbosity', 'minimal'
    )
    Invoke-DotNet @(
        'build',
        '--configuration', $Configuration,
        '--no-restore',
        "-p:ILGPUC_PACKAGE_VERSION=$Version",
        '--nologo', '--verbosity', 'minimal'
    )

    Write-Host '==> run + assert'
    $actualLines = @(& dotnet run --configuration $Configuration --no-build --no-restore)
    if ($LASTEXITCODE -ne 0) {
        throw "HelloKernel failed with exit code $LASTEXITCODE."
    }
    $actualOutput = $actualLines -join "`n"
    if ($actualOutput -cne $expectedOutput) {
        [Console]::Error.WriteLine('FAIL: stdout mismatch')
        Write-Host 'Expected:'
        Write-Host $expectedOutput
        Write-Host 'Actual:'
        Write-Host $actualOutput
        throw 'HelloKernel output did not match expected results.'
    }
}
finally {
    Pop-Location
}

Write-Host '==> verify generated artifacts'
$manifest = Join-Path $projectDir "obj\$Configuration\net10.0\ilgpu\manifest.txt"
$generatedDir = Join-Path $projectDir "obj\$Configuration\net10.0\ilgpu\generated"
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "FAIL: $manifest missing — ILGPUC targets may not have auto-imported."
}
if (-not (Test-Path -LiteralPath $generatedDir -PathType Container)) {
    throw "FAIL: $generatedDir missing — kernel code generation may not have run."
}

Write-Host
Write-Host 'OK'
